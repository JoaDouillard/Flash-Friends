using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FlashFriends
{
    // ─── Résultat de score pour une photo ──────────────────────────────────────

    /// <summary>Détail du calcul de score d'une photo.</summary>
    [System.Serializable]
    public class PhotoScore
    {
        /// <summary>Score brut de cette photo (avant bonus).</summary>
        public int baseScore;

        /// <summary>Bonus accordés sur cette photo (description → valeur).</summary>
        public List<ScoreBonus> bonuses = new List<ScoreBonus>();

        /// <summary>Score total de cette photo (base + somme des bonus).</summary>
        public int totalScore;

        /// <summary>Résultat de la photo associée.</summary>
        public PhotoResult photoResult;
    }

    /// <summary>Un bonus de score avec libellé et valeur.</summary>
    [System.Serializable]
    public class ScoreBonus
    {
        public string label;
        public int    value;

        public ScoreBonus(string label, int value)
        {
            this.label = label;
            this.value = value;
        }
    }

    // ─── PhotoScorer ───────────────────────────────────────────────────────────

    // Calcule le score Good Vibes de chaque photo (base 100 + centrage/distance/taille/type) et cumule le total.
    public class PhotoScorer : MonoBehaviour
    {
        [Header("Paramètres de scoring")]
        [Tooltip("Distance idéale sujet-caméra en mètres. Plus on s'en approche, plus le score est élevé.")]
        [Range(1f, 20f)]
        public float distanceOptimale = 5f;

        [Tooltip("Plage autour de la distance optimale (±) dans laquelle le bonus distance est maximal.")]
        [Range(0.5f, 10f)]
        public float distanceTolerance = 3f;

        [Tooltip("Taille normalisée idéale (fraction de la hauteur d'écran). 0.3 = sujet occupe 30% de l'écran.")]
        [Range(0.05f, 1f)]
        public float sizeOptimale = 0.3f;

        [Tooltip("Tolérance de taille autour de sizeOptimale dans laquelle le bonus taille est maximal.")]
        [Range(0.01f, 0.5f)]
        public float sizeTolerance = 0.15f;

        [Header("Bonus par type de sujet")]
        public int bonusNPC      = 30;
        public int bonusPOI      = 20;
        public int bonusObject   = 10;
        public int bonusLandmark = 15;

        [Header("Bonus multi-sujets")]
        [Tooltip("Points supplémentaires par sujet au-delà du premier.")]
        public int bonusParSujetSupplementaire = 25;

        [Header("Événements")]
        public UnityEvent<PhotoScore> onScoreCalculated;
        public UnityEvent<int> onTotalScoreChanged;

        // ─── État ──────────────────────────────────────────────────────────

        /// <summary>Score Good Vibes cumulé depuis le lancement.</summary>
        public int TotalScore { get; private set; }

        /// <summary>Historique de tous les scores de photos prises.</summary>
        public List<PhotoScore> History { get; } = new List<PhotoScore>();

        // ─── Entrée publique ───────────────────────────────────────────────

        public void AddBonusScore(int amount)
        {
            if (amount <= 0) return;
            TotalScore += amount;
            onTotalScoreChanged?.Invoke(TotalScore);
            Debug.Log($"[PhotoScorer] Quest bonus +{amount} Good Vibes | Total={TotalScore}");
        }

        public void OnPhotoTaken(PhotoResult result)
        {
            PhotoScore score = CalculateScore(result);
            History.Add(score);

            TotalScore += score.totalScore;
            onScoreCalculated?.Invoke(score);
            onTotalScoreChanged?.Invoke(TotalScore);

            // Log de debug
            Debug.Log($"[PhotoScorer] +{score.totalScore} Good Vibes (base={score.baseScore}) | Total={TotalScore}");
            foreach (var b in score.bonuses)
                Debug.Log($"  ↳ {b.label} : +{b.value}");
        }

        // ─── Calcul du score ───────────────────────────────────────────────

        private PhotoScore CalculateScore(PhotoResult result)
        {
            var score = new PhotoScore { photoResult = result };

            if (result.subjects.Count == 0)
            {
                score.baseScore  = 0;
                score.totalScore = 0;
                return score;
            }

            int total = 0;

            // ── Score par sujet ──
            foreach (var data in result.subjects)
            {
                int subjectScore = 100; // base

                // Centrage : distance au centre (0.5, 0.5) → +0 à +50
                float distFromCenter = Vector2.Distance(data.viewportPosition, new Vector2(0.5f, 0.5f));
                // distFromCenter ∈ [0, ~0.71] ; 0 = parfaitement centré
                int centerBonus = Mathf.RoundToInt(Mathf.Lerp(50f, 0f, distFromCenter / 0.5f));
                centerBonus = Mathf.Clamp(centerBonus, 0, 50);

                // Distance optimale : → +0 à +50
                float distDelta = Mathf.Abs(data.distance - distanceOptimale);
                int distBonus = Mathf.RoundToInt(Mathf.Lerp(50f, 0f, distDelta / distanceTolerance));
                distBonus = Mathf.Clamp(distBonus, 0, 50);

                // Taille dans le cadre : → +0 à +50
                float sizeDelta = Mathf.Abs(data.normalizedSize - sizeOptimale);
                int sizeBonus = Mathf.RoundToInt(Mathf.Lerp(50f, 0f, sizeDelta / sizeTolerance));
                sizeBonus = Mathf.Clamp(sizeBonus, 0, 50);

                // Bonus type
                int typeBonus = data.subject.subjectType switch
                {
                    PhotoSubjectType.NPC      => bonusNPC,
                    PhotoSubjectType.POI      => bonusPOI,
                    PhotoSubjectType.Object   => bonusObject,
                    PhotoSubjectType.Landmark => bonusLandmark,
                    _                         => 0
                };

                subjectScore += centerBonus + distBonus + sizeBonus + typeBonus;
                total        += subjectScore;

                score.baseScore += subjectScore;

                // Enregistrement des bonus pour affichage HUD
                score.bonuses.Add(new ScoreBonus($"{data.subject.name} base",      100));
                if (centerBonus > 0) score.bonuses.Add(new ScoreBonus($"{data.subject.name} centering", centerBonus));
                if (distBonus   > 0) score.bonuses.Add(new ScoreBonus($"{data.subject.name} distance",  distBonus));
                if (sizeBonus   > 0) score.bonuses.Add(new ScoreBonus($"{data.subject.name} size",      sizeBonus));
                if (typeBonus   > 0) score.bonuses.Add(new ScoreBonus($"{data.subject.name} type",      typeBonus));
            }

            // ── Bonus multi-sujets ──
            int multiBonus = (result.subjects.Count - 1) * bonusParSujetSupplementaire;
            if (multiBonus > 0)
            {
                score.bonuses.Add(new ScoreBonus($"Multi-subject ×{result.subjects.Count}", multiBonus));
                total += multiBonus;
            }

            score.baseScore  = total;
            score.totalScore = total;
            return score;
        }
    }
}
