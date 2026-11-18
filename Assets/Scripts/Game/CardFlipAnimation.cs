using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // Gira cada carta sobre su centro sin modificar la distribución de la cuadrícula.
    public sealed class CardFlipAnimation
    {
        private readonly List<CardRotation> outgoingCards = new List<CardRotation>(16);
        private readonly List<CardRotation> incomingCards = new List<CardRotation>(16);

        private struct CardRotation
        {
            public Transform Transform;
            public Quaternion OriginalRotation;
        }

        public IEnumerator Play(GameObject fronts, GameObject backs, bool reveal, float duration)
        {
            Restore();
            GameObject outgoing = reveal ? backs : fronts;
            GameObject incoming = reveal ? fronts : backs;

            if (duration <= 0f)
            {
                outgoing.SetActive(false);
                incoming.SetActive(true);
                yield break;
            }

            Capture(outgoing.transform, outgoingCards);
            Capture(incoming.transform, incomingCards);
            incoming.SetActive(false);
            outgoing.SetActive(true);

            float halfDuration = duration * 0.5f;
            for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                Rotate(outgoingCards, Mathf.Lerp(0f, 90f, progress));
                yield return null;
            }

            // Cambia entre frontal y reverso cuando están de canto para ocultar la sustitución.
            Rotate(outgoingCards, 90f);
            Rotate(incomingCards, -90f);
            outgoing.SetActive(false);
            incoming.SetActive(true);

            for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                Rotate(incomingCards, Mathf.Lerp(-90f, 0f, progress));
                yield return null;
            }

            Restore();
        }

        // También se llama al interrumpir la ronda para que ninguna carta quede de canto.
        public void Restore()
        {
            Rotate(outgoingCards, 0f);
            Rotate(incomingCards, 0f);
            outgoingCards.Clear();
            incomingCards.Clear();
        }

        private static void Capture(Transform container, List<CardRotation> cards)
        {
            for (int i = 0; i < container.childCount; i++)
            {
                Transform card = container.GetChild(i);
                if (!card.gameObject.activeSelf) continue;
                cards.Add(new CardRotation { Transform = card, OriginalRotation = card.localRotation });
            }
        }

        private static void Rotate(List<CardRotation> cards, float angle)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            foreach (CardRotation card in cards)
            {
                if (card.Transform != null)
                    card.Transform.localRotation = card.OriginalRotation * rotation;
            }
        }
    }
}
