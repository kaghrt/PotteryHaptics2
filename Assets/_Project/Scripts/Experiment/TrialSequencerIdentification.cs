using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// 弾性/粘性の4刺激(強弱×2)をランダムに提示し、「弾性/粘性のどちらだったか」を2AFCで回答させる。
    /// </summary>
    public class TrialSequencerIdentification : MonoBehaviour
    {
        [SerializeField] private ElasticitySurface elasticitySurface;
        [SerializeField] private ViscositySurface viscositySurface;
        [SerializeField] private ResponseUI responseUI;
        [SerializeField] private MonoBehaviour visualConditionSwitcherBehaviour;
        [SerializeField] private StimulusDefinition elasticityWeakStimulus;
        [SerializeField] private StimulusDefinition elasticityStrongStimulus;
        [SerializeField] private StimulusDefinition viscosityWeakStimulus;
        [SerializeField] private StimulusDefinition viscosityStrongStimulus;
        [SerializeField] private int trialsPerStimulus = 8;
        [SerializeField] private float presentationDurationSec = 3f;
        [SerializeField] private string phaseName = "Identification";

        private IVisualConditionSwitcher visualConditionSwitcher;
        private TrialLogger logger;
        private int trialIndex;

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

            StimulusDefinition[] stimulusSet =
            {
                elasticityWeakStimulus, elasticityStrongStimulus,
                viscosityWeakStimulus, viscosityStrongStimulus
            };

            for (int half = 0; half < 2; half++)
            {
                bool isSecondHalf = half == 1;
                VisualCondition condition = ExperimentManager.Instance != null
                    ? ExperimentManager.Instance.GetVisualCondition(isSecondHalf)
                    : VisualCondition.Minimal;

                visualConditionSwitcher?.SetVisualCondition(condition);

                List<StimulusDefinition> trials = BuildTrialList(stimulusSet);
                foreach (StimulusDefinition stimulus in trials)
                {
                    yield return RunSingleTrial(stimulus, condition);
                }
            }

            logger.Close();

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.NextScene();
            }
        }

        private List<StimulusDefinition> BuildTrialList(StimulusDefinition[] stimulusSet)
        {
            var trials = new List<StimulusDefinition>();
            foreach (StimulusDefinition stimulus in stimulusSet)
            {
                for (int i = 0; i < trialsPerStimulus; i++)
                {
                    trials.Add(stimulus);
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

        private IEnumerator RunSingleTrial(StimulusDefinition stimulus, VisualCondition condition)
        {
            bool isElasticity = stimulus.TextureType == TextureType.Elasticity;

            elasticitySurface.ActiveStimulus = isElasticity ? stimulus : null;
            viscositySurface.ActiveStimulus = isElasticity ? null : stimulus;

            yield return new WaitForSeconds(presentationDurationSec);

            elasticitySurface.ActiveStimulus = null;
            viscositySurface.ActiveStimulus = null;

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
            responseUI.PromptResponse(
                "elasticity", "弾性(押すと硬さがある)",
                "viscosity", "粘性(押すとねばりがある)");

            while (!responded)
            {
                yield return null;
            }

            responseUI.OnResponse -= HandleResponse;

            bool correct = (isElasticity && responseValue == "elasticity") ||
                           (!isElasticity && responseValue == "viscosity");

            trialIndex++;
            logger.WriteRecord(new TrialRecord
            {
                participantId = ExperimentManager.Instance != null ? ExperimentManager.Instance.ParticipantId : "unknown",
                phase = phaseName,
                visualCondition = condition,
                trialIndex = trialIndex,
                textureType = stimulus.TextureType,
                stimulusName = stimulus.DisplayName,
                physicalValue = stimulus.PhysicalValue,
                relativeIntensityPercent = stimulus.RelativeIntensityPercent,
                intensityLevel = stimulus.IntensityLevel,
                responseValue = responseValue,
                correct = correct,
                reactionTimeSec = reactionTimeSec,
                timestampIso = DateTime.UtcNow.ToString("o")
            });
        }
    }
}
