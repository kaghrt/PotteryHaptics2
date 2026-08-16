using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// 恒常法(method of constant stimuli)による弾性/粘性JND測定の試行列を生成・進行する。
    /// 基準刺激と比較刺激(5段階)を順に提示し、「1回目/2回目どちらが強かったか」を2AFCで回答させる。
    /// </summary>
    public class TrialSequencerJND : MonoBehaviour
    {
        [SerializeField] private VirtualSurfaceBase surface;
        [SerializeField] private ResponseUI responseUI;
        [SerializeField] private MonoBehaviour visualConditionSwitcherBehaviour;
        [SerializeField] private StimulusDefinition standardStimulus;
        [SerializeField] private StimulusDefinition[] comparisonStimuli;
        [SerializeField] private int trialsPerLevel = 20;
        [SerializeField] private float presentationDurationSec = 2f;
        [SerializeField] private float interStimulusIntervalSec = 0.5f;
        [SerializeField] private string phaseName = "JND";

        private IVisualConditionSwitcher visualConditionSwitcher;
        private TrialLogger logger;
        private int trialIndex;

        private struct PlannedTrial
        {
            public StimulusDefinition comparisonStimulus;
            public bool standardFirst;
        }

        private void Awake()
        {
            visualConditionSwitcher = visualConditionSwitcherBehaviour as IVisualConditionSwitcher;
        }

        private void Start()
        {
            StartCoroutine(RunSession());
        }

        private IEnumerator RunSession()
        {
            string participantId = ExperimentManager.Instance != null ? ExperimentManager.Instance.ParticipantId : "unknown";
            logger = new TrialLogger(participantId, phaseName);
            logger.Open();
            trialIndex = 0;

            for (int half = 0; half < 2; half++)
            {
                bool isSecondHalf = half == 1;
                VisualCondition condition = ExperimentManager.Instance != null
                    ? ExperimentManager.Instance.GetVisualCondition(isSecondHalf)
                    : VisualCondition.Minimal;

                visualConditionSwitcher?.SetVisualCondition(condition);

                List<PlannedTrial> trials = BuildTrialList();
                foreach (PlannedTrial trial in trials)
                {
                    yield return RunSingleTrial(trial, condition);
                }
            }

            logger.Close();

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.NextScene();
            }
        }

        private List<PlannedTrial> BuildTrialList()
        {
            var trials = new List<PlannedTrial>();
            foreach (StimulusDefinition comparison in comparisonStimuli)
            {
                for (int i = 0; i < trialsPerLevel; i++)
                {
                    trials.Add(new PlannedTrial
                    {
                        comparisonStimulus = comparison,
                        standardFirst = UnityEngine.Random.value < 0.5f
                    });
                }
            }

            Shuffle(trials);
            return trials;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private IEnumerator RunSingleTrial(PlannedTrial trial, VisualCondition condition)
        {
            StimulusDefinition first = trial.standardFirst ? standardStimulus : trial.comparisonStimulus;
            StimulusDefinition second = trial.standardFirst ? trial.comparisonStimulus : standardStimulus;

            surface.ActiveStimulus = first;
            yield return new WaitForSeconds(presentationDurationSec);
            surface.ActiveStimulus = null;
            yield return new WaitForSeconds(interStimulusIntervalSec);

            surface.ActiveStimulus = second;
            yield return new WaitForSeconds(presentationDurationSec);
            surface.ActiveStimulus = null;

            bool responded = false;
            string responseValue = null;
            float reactionTimeSec = 0f;

            void HandleResponse(string value, float rt)
            {
                responseValue = value;
                reactionTimeSec = rt;
                responded = true;
            }

            responseUI.OnResponse += HandleResponse;
            responseUI.PromptResponse("first", "1回目の方が強かった", "second", "2回目の方が強かった");

            while (!responded)
            {
                yield return null;
            }

            responseUI.OnResponse -= HandleResponse;

            bool correct = EvaluateCorrect(trial, responseValue);

            trialIndex++;
            logger.WriteRecord(new TrialRecord
            {
                participantId = ExperimentManager.Instance != null ? ExperimentManager.Instance.ParticipantId : "unknown",
                phase = phaseName,
                visualCondition = condition,
                trialIndex = trialIndex,
                textureType = trial.comparisonStimulus.TextureType,
                stimulusName = trial.comparisonStimulus.DisplayName,
                physicalValue = trial.comparisonStimulus.PhysicalValue,
                relativeIntensityPercent = trial.comparisonStimulus.RelativeIntensityPercent,
                intensityLevel = trial.comparisonStimulus.IntensityLevel,
                responseValue = responseValue,
                correct = correct,
                reactionTimeSec = reactionTimeSec,
                timestampIso = DateTime.UtcNow.ToString("o")
            });
        }

        /// <summary>
        /// 比較刺激の相対%の符号と回答の対応で正誤判定する。
        /// 0%(基準と同一)の試行は正誤の概念がないため常にfalse扱い(バイアス確認用データとして分析側で使う)。
        /// </summary>
        private bool EvaluateCorrect(PlannedTrial trial, string responseValue)
        {
            float relativePercent = trial.comparisonStimulus.RelativeIntensityPercent;
            if (Mathf.Approximately(relativePercent, 0f))
            {
                return false;
            }

            bool comparisonIsStronger = relativePercent > 0f;
            string comparisonPosition = trial.standardFirst ? "second" : "first";
            string standardPosition = trial.standardFirst ? "first" : "second";
            string expectedResponse = comparisonIsStronger ? comparisonPosition : standardPosition;
            return responseValue == expectedResponse;
        }
    }
}
