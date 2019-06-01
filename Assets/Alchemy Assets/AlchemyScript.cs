using KModkit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

public class AlchemyScript : MonoBehaviour {
    public KMAudio BombAudio;
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMSelectable[] FreqButtons;
    public KMSelectable[] SymbolButtons;
    public KMSelectable ModuleSelect, ClearButton, SubmitButton, ReDrawButton;
    public SpriteRenderer[] Symbols;
    public Sprite[] SymbolSprites;
    public GameObject MainCircleSymbol;
    public TextMesh MainCircleText;

    readonly int[] nowSymbols = new int[6];
    readonly string[] freqNames = { "Mind", "Flames", "Matter", "Energy", "Life" };
    readonly string[] symbolNames = { "Creation", "Fire", "Heva", "Meta", "Strucota", "Terra" };

    List<int> pickedValues = new List<int>();
    List<int> completeSol = new List<int>();
    int mainSymbol, correctFreq, currentStep, nowStep;
    delegate bool checkLogic();
    checkLogic[] getLogic;

    bool moduleSolved;
    static int moduleIDCounter = 1;
    int moduleid;

    void Start() {
        moduleid = moduleIDCounter++;
        SetMainSymbol();
        getLogic = new checkLogic[] {
            () => Enumerable.Range(0, SymbolButtons.Length).Any(a => (nowSymbols[a] == 0 && nowSymbols[(a + 3) % SymbolButtons.Length] == 5)),
            () => Enumerable.Range(0, SymbolButtons.Length).Any(a => (nowSymbols[a] == 3 && (nowSymbols[(a + 1) % SymbolButtons.Length] == 2 || nowSymbols[(a + 5) % SymbolButtons.Length] == 2))),
            () => (nowSymbols[4] == 1 && int.Parse(BombInfo.GetSerialNumber().Last().ToString()) % 2 == 0)
        };

        for (var i = 0; i < FreqButtons.Length; i++) {
            var j = i;

            FreqButtons[i].OnInteract += delegate() {
                OnFreqPress(j);

                return false;
            };
        }

        ClearButton.OnInteract += delegate() {
            BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            ModuleSelect.AddInteractionPunch(0.1f);
            MainCircleText.text = "";

            return false;
        };

        for (var i = 0; i < SymbolButtons.Length; i++) {
            var j = i;

            SymbolButtons[i].OnInteract += delegate() {
                OnSymbolPress(j);

                return false;
            };
        }

        SubmitButton.OnInteract += delegate() {
            OnSubmitPress();

            return false;
        };

        ReDrawButton.OnInteract += delegate() {
            OnReDrawPress();

            return false;
        };

        ReDraw();
    }

    void Update() {
        MainCircleSymbol.transform.Rotate(Vector3.back * 1f * Time.deltaTime);
    }

    int ChooseUnique(int maxVal) {
        var nowVal = 0;

        do {
            nowVal = Random.Range(0, maxVal);
        } while (pickedValues.Contains(nowVal));

        pickedValues.Add(nowVal);

        return nowVal;
    }

    void SetMainSymbol() {
        mainSymbol = Random.Range(0, SymbolSprites.Length);
        MainCircleSymbol.GetComponent<SpriteRenderer>().sprite = SymbolSprites[mainSymbol];
        Debug.LogFormat(@"[Alchemy #{0}] The symbol is {1}.", moduleid, symbolNames[mainSymbol]);
    }

    void OnFreqPress(int freqPressed) {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch(0.1f);
        MainCircleText.text = freqNames[freqPressed];
    }

    void OnSymbolPress(int symbolPressed) {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch(0.15f);

        if (moduleSolved) return;

        if (completeSol[nowStep] != nowSymbols[symbolPressed] || (correctFreq == -1 && MainCircleText.text != "") || correctFreq != Array.IndexOf(freqNames, MainCircleText.text)) {
            OnReset();
        } else if (nowStep + 1 == completeSol.Count) {
            MainCircleText.text = "";
            CheckSteps();
        } else {
            nowStep++;
        }
    }

    void OnSubmitPress() {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch(0.5f);

        if (moduleSolved) return;

        if (completeSol[nowStep] != 7) {
            OnReset();

            return;
        }

        correctFreq = -1;
        nowStep = 0;
        BombModule.HandlePass();
        moduleSolved = true;
        Debug.LogFormat(@"[Alchemy #{0}] Module solved!", moduleid);
    }

    void OnReDrawPress() {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch(0.5f);

        if (moduleSolved) return;

        if (completeSol[nowStep] != 6 || (correctFreq == -1 && MainCircleText.text != "") || correctFreq != Array.IndexOf(freqNames, MainCircleText.text)) {
            OnReset();

            return;
        }

        ReDraw();
    }

    void ReDraw() {
        Debug.LogFormat(@"[Alchemy #{0}] Drawing a new circle.", moduleid);

        for (int i = 0; i < Symbols.Length; i++) {
            nowSymbols[i] = ChooseUnique(SymbolSprites.Length);
            Symbols[i].sprite = SymbolSprites[nowSymbols[i]];
        }

        pickedValues.Clear();
        MainCircleText.text = "";

        if (currentStep == 0) {
            SetMainSymbol();
            var notPerfect = getLogic.Any(x => x());
            Debug.LogFormat(@"[Alchemy #{0}] Circle is {1}", moduleid, (notPerfect) ? "imperfect" : "perfect");
            currentStep = (notPerfect) ? 0 : 1;
        }

        CheckSteps();
    }

    void CheckSteps() {
        completeSol.Clear();
        nowStep = 0;
        correctFreq = -1;

        switch (currentStep) {
            case 0:
                var checkSymbols = new[] { 2, 0, 5, 4, 1, -1 };
                var checkPositions = new[] { 3, 0, 1, 4, 5 };
                var setFreq = new[] { 0, 4, 3, 2, 1 };

                for (int i = 0; i < checkSymbols.Length; i++) {
                    if (i == checkSymbols.Length - 1) {
                        correctFreq = -1;

                        break;
                    }

                    if (nowSymbols[checkPositions[i]] == checkSymbols[i]) {
                        correctFreq = setFreq[i];

                        break;
                    }
                }

                completeSol.Add(6);
                break;

            case 1:
                if (mainSymbol == 1) { //If the circle already has FIRE, go to step 4.
                    currentStep = 3;
                } else if (mainSymbol == 3) { //Otherwise, if the circle already has META, go to step 3.
                    currentStep = 2;
                } else if (mainSymbol == 4) { //Otherwise, if the circle already has STRUCOTA, go to step 6.
                    currentStep = 5;
                } else if (mainSymbol == 2) { //Otherwise, if the circle already has HEVA, go to step 5.
                    currentStep = 4;
                } else if (mainSymbol == 0) { //Otherwise, if the circle already has CREATION, press Submit.
                    completeSol.Add(7);
                    LogSteps();

                    return;
                } else { //Otherwise, if none apply press FIRE and go to step 4.
                    completeSol.Add(1);
                    currentStep = 3;
                    LogSteps();

                    return;
                }

                CheckSteps();

                return;

            case 2:
                if (mainSymbol == 5) { //If the circle contains TERRA, press LIFE, and then press HEVA and go to step 9.
                    correctFreq = 4;
                    completeSol.Add(2);
                    currentStep = 8;
                } else if (mainSymbol == 3) { //Otherwise, if the circle contains META, press HEVA and go to step 6.
                    completeSol.Add(2);
                    currentStep = 5;
                } else if (mainSymbol == 1) { //Otherwise, if the circle contains FIRE, press MIND, and then press STRUCOTA and go to step 7.
                    correctFreq = 0;
                    completeSol.Add(4);
                    currentStep = 6;
                } else { //Otherwise, press re-draw and go to step 4.
                    completeSol.Add(6);
                    currentStep = 3;
                }
                break;

            case 3:
                if (BombInfo.IsIndicatorOn("TRN")) { //If there is a lit TRN indicator on the bomb, press ENERGY, re-draw the circle, and go to step 7.
                    correctFreq = 3;
                    completeSol.Add(6);
                    currentStep = 6;
                } else { //Otherwise, go to step 5.
                    currentStep = 4;
                    CheckSteps();

                    return;
                }
                break;

            case 4:
                if (mainSymbol == 5) { //If the circle contains TERRA, press ENERGY, and then press re-draw and go to step 11.
                    correctFreq = 3;
                    completeSol.Add(6);
                    currentStep = 10;
                } else if (mainSymbol == 0) { //If the circle contains CREATION, press FIRE and go to step 8.
                    completeSol.Add(1);
                    currentStep = 7;
                } else if (mainSymbol == 1) { //Otherwise, if the circle contains FIRE, press LIFE, and then press STRUCOTA and go to step 10.
                    correctFreq = 4;
                    completeSol.Add(4);
                    currentStep = 9;
                } else { //Otherwise, press re-draw and go to step 6.
                    completeSol.Add(6);
                    currentStep = 5;
                }
                break;

            case 5:
                if (BombInfo.IsIndicatorOn("SND")) { //If there is a lit SND indicator on the bomb, press LIFE, re-draw the circle, and go to step 9.
                    correctFreq = 4;
                    completeSol.Add(6);
                    currentStep = 8;
                } else { //Otherwise, go to step 11.
                    currentStep = 10;
                    CheckSteps();

                    return;
                }
                break;

            case 6: //Press TERRA and press Submit.
                completeSol.Add(5);
                completeSol.Add(7);
                break;

            case 7: //Press CREATION and press Submit. 
                completeSol.Add(0);
                completeSol.Add(7);
                break;

            case 8: //Press TERRA and press Submit.
                completeSol.Add(5);
                completeSol.Add(7);
                break;

            case 9: //Press CREATION and press Submit.
                completeSol.Add(0);
                completeSol.Add(7);
                break;

            case 10: //Press HEVA and Submit.
                completeSol.Add(2);
                completeSol.Add(7);
                break;
        }

        LogSteps();
    }

    void LogSteps() {
        var buttonNames = new[] { "None", "Creation", "Fire", "Heva", "Meta", "Strucota", "Terra", "ReDraw", "Submit" };
        Debug.LogFormat(@"[Alchemy #{0}] Frequency to press is {1}, button{2} to press {3} {4}", moduleid, (correctFreq != -1) ? freqNames[correctFreq] : "None", (completeSol.Count == 1) ? "" : "s", (completeSol.Count == 1) ? "is" : "are", string.Join(", ", completeSol.Select(x => buttonNames[x + 1]).ToArray()));
    }

    void OnReset() {
        BombModule.HandleStrike();
        Debug.LogFormat(@"[Alchemy #{0}] An strike has occurred, resetting module.", moduleid);
        MainCircleText.text = "";
        correctFreq = -1;
        currentStep = nowStep = 0;
        completeSol.Clear();
        ReDraw();
    }
}