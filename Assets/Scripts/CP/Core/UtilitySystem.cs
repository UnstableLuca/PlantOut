using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace AI.Core
{
    public class UtilitySystem<T>
    {
        private class Option
        {
            public string Name;
            public T Value;
            public Func<float> Score;
            public float LastScore;
        }

        private readonly List<Option> _options = new List<Option>();

        public UtilitySystem<T> Add(string name, T value, Func<float> score)
        {
            _options.Add(new Option { Name = name, Value = value, Score = score });
            return this;
        }
        //Devuelve la mejor opcion con mayor puntuacion, si no cumple min da false
        public bool TryGetBest(out T best, out float bestScore, float minScore = 0f)
        {
            best = default;
            bestScore = float.NegativeInfinity;
            bool found = false;

            foreach (var o in _options)
            {
                o.LastScore = o.Score();
                if (o.LastScore > bestScore && o.LastScore >= minScore)
                {
                    bestScore = o.LastScore;
                    best = o.Value;
                    found = true;
                }
            }
            return found;
        }

        //Texto ultimas puntuaciones, para depurar
        public string Describe()
        {
            var sb = new StringBuilder();
            foreach (var o in _options) sb.AppendLine($"{o.Name}: {o.LastScore:0.00}");
            return sb.ToString();
        }
    }

    public static class UtilitySelector
    {
        public static bool TrySelectBest<T>(IEnumerable<T> candidates, Func<T, float> score, out T best,
                                            float minScore = float.NegativeInfinity)
        {
            best = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            foreach (var c in candidates)
            {
                float s = score(c);
                if (s > bestScore && s >= minScore)
                {
                    bestScore = s;
                    best = c;
                    found = true;
                }
            }
            return found;
        }
    }

    public static class UtilityCurves
    {
        public static float Linear(float x, float min, float max) => Mathf.Clamp01(Mathf.InverseLerp(min, max, x));

        public static float Inverse(float x, float min, float max) => 1f - Linear(x, min, max);

        public static float Quadratic(float x, float min, float max)
        {
            float t = Linear(x, min, max);
            return t * t;
        }

        public static float Logistic(float x, float midpoint, float steepness = 1f) =>
           1f / (1f + Mathf.Exp(-steepness * (x - midpoint)));

        public static float FromCurve(AnimationCurve curve, float x, float min, float max) =>
            Mathf.Clamp01(curve.Evaluate(Linear(x, min, max)));
    }
}