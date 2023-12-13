using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;

namespace CardHouse.SampleGames.Tarot
{
    public class SpreadManager : MonoBehaviour
    {
        public Text SpreadLabel;
        public CardGroup Deck;
        public GameObject SpreadOrderLabelPrefab;
        public TMP_Text Key;

        public List<TarotSpread> Spreads;
        List<GameObject> CurrentSpreadLabels = new List<GameObject>();

        int CurrentSpreadIndex = 0;

        void Start()
        {
            foreach (var spread in Spreads)
            {
                foreach (var slot in spread.Slots)
                {
                    slot.gameObject.SetActive(false);
                }
            }
            AdjustSpread(0);
        }

        public void NextSpread()
        {
            AdjustSpread(1);
        }

        public void PreviousSpread()
        {
            AdjustSpread(-1);
        }

        void AdjustSpread(int diff)
        {
            ShuffleCardsBackIn();

            foreach (var label in CurrentSpreadLabels)
            {
                Destroy(label);
            }

            foreach (var slot in Spreads[CurrentSpreadIndex].Slots)
            {
                slot.gameObject.SetActive(false);
            }

            CurrentSpreadIndex = (CurrentSpreadIndex + diff) % Spreads.Count;
            while (CurrentSpreadIndex < 0)
            {
                CurrentSpreadIndex += Spreads.Count;
            }
            SpreadLabel.text = Spreads[CurrentSpreadIndex].Name;
            Key.text = Spreads[CurrentSpreadIndex].Instructions;

            CurrentSpreadLabels.Clear();
            for (var i = 0; i < Spreads[CurrentSpreadIndex].Slots.Count; i++)
            {
                var slot = Spreads[CurrentSpreadIndex].Slots[i];
                slot.gameObject.SetActive(true);
                var label = Instantiate(SpreadOrderLabelPrefab, slot.transform);
                label.GetComponent<TMP_Text>().text = (i + 1).ToString();
                CurrentSpreadLabels.Add(label);
            }
        }

        public void ShuffleCardsBackIn()
        {
            var areCardsInPlay = false;
            foreach (var slot in Spreads[CurrentSpreadIndex].Slots)
            {
                foreach (var card in slot.MountedCards.ToList())
                {
                    Deck.Mount(card);
                    areCardsInPlay = true;
                }
            }

            if (areCardsInPlay)
            {
                Deck.Shuffle();
            }
        }

        public void DealNextCard()
        {
            if (Deck.MountedCards.Count == 0)
                return;

            Spreads[CurrentSpreadIndex].FillNext(Deck.MountedCards[Deck.MountedCards.Count - 1]);
			
			if (Spreads[CurrentSpreadIndex].IsComplete())
				
			{
				StartCoroutine(SendTarotData(Spreads[CurrentSpreadIndex]));
			}
		}
        private IEnumerator <object> SendTarotData(TarotSpread spread)
        {
            var tarotData = new
            {
                spreadName = spread.Name,
                cards = spread.Slots.Select((slot, index) => new
                {
                    position = index,
                    cardName = slot.Card.name,
                    orientation = slot.Card.transform.rotation.eulerAngles
                })
            };

            var json = JsonUtility.ToJson(tarotData);

            var request = new UnityWebRequest("https://pflask.onrender.com/interpret", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log(request.error);
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
            }
        }
    }
}