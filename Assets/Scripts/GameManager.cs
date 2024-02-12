using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPartyGames.BubbleShooter
{
	public class GameManager : MonoBehaviour
	{
		#region Singleton
		public static GameManager instance;

		private void Awake()
		{
			if (instance == null)
				instance = this;

			winMenu.SetActive(false);
			LoseMenu.SetActive(false);
			levelsUI.SetActive(false);
            StartTimer();
            sequenceBubbles = new List<Transform>();
			connectedBubbles = new List<Transform>();
			bubblesToDrop = new List<Transform>();
			bubblesToDissolve = new List<Transform>();
			DontDestroyOnLoad(gameObject);
		}
		#endregion

		private const int SEQUENCE_SIZE = 3;

		private List<Transform> sequenceBubbles;
		private List<Transform> connectedBubbles;
		private List<Transform> bubblesToDrop;
		private List<Transform> bubblesToDissolve;
		public Shooter shootScript;
		public GameObject winMenu;
		public GameObject LoseMenu;
		public GameObject winScore;
		public GameObject winThrows;
		public GameObject volBtn;
		public GameObject playBtn;
		public GameObject homeVolBtn;
		public GameObject startUI;
		public GameObject levelsUI;
		public GameObject LightObj;
		public Transform bottomLimit;
		public float dropSpeed = 50f;
		public string gameState = "play";
		public bool isDissolving = false;
		public float dissolveSpeed = 2f;
		public float RayDistance = 200f;
		[SerializeField] private float gameDuration = 120f;
		[SerializeField] private Text timerText;
		[SerializeField] private bool gameOver = false;
		private float timer;

		private void Update()
		{
			
			if (timer > 0 )

			{
				timer -= Time.deltaTime;
				UpdateTimerUI();

            }
			else
            {
				
				if(!gameOver)
                {
					gameOver = true;
					timer = 0f;
					UpdateTimerUI();
					LevelManager.instance.ClearLevel();
					shootScript.canShoot = false;
					ScoreManager man = ScoreManager.GetInstance();
					winScore.GetComponent<Text>().text = man.GetScore().ToString();
					winThrows.GetComponent<Text>().text = man.GetThrows().ToString();
					winMenu.SetActive(true);
					Debug.Log("Game Over" + gameOver);
				}
			

			}

			if (isDissolving)
			{
				foreach (Transform bubble in bubblesToDissolve)
				{

					if (bubble == null)
					{
						//make sure every bubble disappeared before ending the dissolve
						if (bubblesToDissolve.IndexOf(bubble) == bubblesToDissolve.Count - 1)
						{
							isDissolving = false;
							EmptyDissolveList();
							break;
						}
						else continue;
					}

					SpriteRenderer spriteRenderer = bubble.GetComponent<SpriteRenderer>();
					float dissolveAmount = spriteRenderer.material.GetFloat("_DissolveAmount");

					if (dissolveAmount >= 0.99f)
					{
						isDissolving = false;
						EmptyDissolveList();
						break;
					}
					else
					{
						float newDissolve = dissolveAmount + dissolveSpeed * Time.deltaTime;
						spriteRenderer.material.SetFloat("_DissolveAmount", newDissolve);
					}
				}
			}
		}

		private void EmptyDissolveList()
		{
			foreach (Transform bubble in bubblesToDissolve)
				if (bubble != null) Destroy(bubble.gameObject);

			bubblesToDissolve.Clear();
		}

        public void ToggleGameState()
        {
            if (gameState == "play")
            {
                playBtn.GetComponent<Image>().color = Color.gray;
                gameState = "pause";
                LightObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity = 0.1f;
                PauseGame();
            }
            else if (gameState == "pause")
            {
                playBtn.GetComponent<Image>().color = Color.white;
                gameState = "play";
                LightObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity = 1;
				ResumeGame();
            }
        }

        public void RestartGame()
		{
			LevelManager.instance.ClearLevel();
			shootScript.canShoot = false;
			startUI.SetActive(true);
			gameState = "play";
			timer = gameDuration;
			UpdateTimerUI();
			gameOver = false;

		}

		public void RestartGameGameOver()
		{
			RestartGame();
			winMenu.SetActive(false);
			LoseMenu.SetActive(false);

		}


		public void PauseGame()
		{
			Time.timeScale = 0f;
		}

		public void ResumeGame()
		{
			Time.timeScale = 1f;
		}

		public void ToggleMute()
		{
			AudioManager.instance.ToggleMute();
			if (AudioManager.instance.mute)
			{
				volBtn.GetComponent<Image>().color = Color.gray;
				homeVolBtn.GetComponent<Image>().color = Color.gray;
			}
			else
			{
				volBtn.GetComponent<Image>().color = Color.white;
				homeVolBtn.GetComponent<Image>().color = Color.white;
			}
		}

		IEnumerator CheckSequence(Transform currentBubble)
		{
			yield return new WaitForSeconds(0.1f);

			sequenceBubbles.Clear();
			CheckBubbleSequence(currentBubble);
			
			if (sequenceBubbles.Count >= SEQUENCE_SIZE )

			{
				ProcessBubblesInSequence();
				ProcessDisconectedBubbles();
			}

			sequenceBubbles.Clear();
			LevelManager.instance.UpdateListOfBubblesInScene();

			if (LevelManager.instance.bubblesInScene.Count == 0)
			{
				ScoreManager man = ScoreManager.GetInstance();
				winScore.GetComponent<Text>().text = man.GetScore().ToString();
				winThrows.GetComponent<Text>().text = man.GetThrows().ToString();
				timer = 0;
				winMenu.SetActive(true);
			}
			else
			{
				shootScript.CreateNextBubble();
				shootScript.canShoot = true;
			}

			ProcessBottomLimit();
		}

		public void ProcessTurn(Transform currentBubble)
		{
			StartCoroutine(CheckSequence(currentBubble));
		}

		private void ProcessBottomLimit()
		{
			foreach (Transform t in LevelManager.instance.bubblesArea)
			{
				if (t.GetComponent<Bubble>().isConnected
				&& t.position.y < bottomLimit.position.y)
				{
					LevelManager.instance.ClearLevel();
					timer = 0f;
					UpdateTimerUI();
					LoseMenu.SetActive(true);
					shootScript.canShoot = false;
					break;
				}
			}
		}

		private void CheckBubbleSequence(Transform currentBubble)
		{
			sequenceBubbles.Add(currentBubble);

			Bubble bubbleScript = currentBubble.GetComponent<Bubble>();
			List<Transform> neighbours = bubbleScript.GetNeighbours();
			//Debug.Log("Checking sequence for bubble: " + currentBubble.position);
			foreach (Transform t in neighbours)
			{
				if (!sequenceBubbles.Contains(t))
				{
					Bubble bScript = t.GetComponent<Bubble>();

					if (bScript.bubbleColor == bubbleScript.bubbleColor)
					{
						CheckBubbleSequence(t);
					}
				}
			}
			//Debug.Log("Sequence bubbles count: " + sequenceBubbles.Count);
		
	    }

   
        private void ProcessBubblesInSequence()
		{
			
				AudioManager.instance.PlaySound("destroy");

			foreach (Transform t in sequenceBubbles)
			{
				//Debug.Log("Processing bubble for dissolution: " + t.position);
				if (!bubblesToDissolve.Contains(t))
				{
					ScoreManager.GetInstance().AddScore(1);
					t.tag = "Untagged";
					t.SetParent(null);
					t.GetComponent<CircleCollider2D>().enabled = false;
					bubblesToDissolve.Add(t);
				}
			}
			//Debug.Log("Bubbles to dissolve count: " + bubblesToDissolve.Count);
			isDissolving = true;
		}

		#region Drop Disconected Bubbles

		private void ProcessDisconectedBubbles()
		{
			SetAllBubblesConnectionToFalse();
			SetConnectedBubblesToTrue();
			CheckDisconectedBubbles();
			DropAll();
		}

		private void SetAllBubblesConnectionToFalse()
		{
			foreach (Transform bubble in LevelManager.instance.bubblesArea)
			{
				bubble.GetComponent<Bubble>().isConnected = false;
			}
		}

		private void SetConnectedBubblesToTrue()
		{
			connectedBubbles.Clear();

			RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.right, RayDistance);

			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].transform.gameObject.tag.Equals("Bubble"))
					SetNeighboursConnectionToTrue(hits[i].transform);
			}
		}

		private void SetNeighboursConnectionToTrue(Transform bubble)
		{
			connectedBubbles.Add(bubble);

			Bubble bubbleScript = bubble.GetComponent<Bubble>();
			bubbleScript.isConnected = true;

			foreach (Transform t in bubbleScript.GetNeighbours())
			{
				if (!connectedBubbles.Contains(t))
				{
					SetNeighboursConnectionToTrue(t);
				}
			}
		}

		private void CheckDisconectedBubbles()
		{
			foreach (Transform bubble in LevelManager.instance.bubblesArea)
			{
				Bubble bubbleScript = bubble.GetComponent<Bubble>();
                //Debug.Log("----The current bundle  is not avaliable  ");

                //if (bubbleScript != null)
                //            {
                //	//Debug.Log("----The current bundle  is avaliable ");
                //	if (!bubbleScript.isConnected)
                //	{
                //		if (!bubblesToDrop.Contains(bubble))
                //		{
                //			ScoreManager.GetInstance().AddScore(2);
                //			bubble.tag = "Untagged";
                //			bubblesToDrop.Add(bubble);
                //		}
                //	}
                //}
                if (!bubbleScript.isConnected)
                {
                    if (!bubblesToDrop.Contains(bubble))
                    {
                        ScoreManager.GetInstance().AddScore(2);
                        bubble.tag = "Untagged";
                        bubblesToDrop.Add(bubble);
                    }
                }
            }
		}

		private void DropAll()
		{
			foreach (Transform bubble in bubblesToDrop)
			{
				bubble.SetParent(null);
				//Destroy(bubble.gameObject);
				bubble.gameObject.GetComponent<CircleCollider2D>().enabled = false;
				if (!bubble.GetComponent<Rigidbody2D>())
				{
					Rigidbody2D rig = (Rigidbody2D)bubble.gameObject.AddComponent(typeof(Rigidbody2D));
					rig.gravityScale = dropSpeed;
				}
			}
			bubblesToDrop.Clear();
		}

		public void StartTimer()
		{
			timer = gameDuration;
			UpdateTimerUI();
		}

		private void UpdateTimerUI()
		{
			//timerText.text = "Time: " + Mathf.CeilToInt(timer).ToString();
			int minutes = Mathf.FloorToInt(timer / 60);
			int seconds = Mathf.CeilToInt(timer % 60);

			timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
		}

		#endregion
		public void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawRay(transform.position, Vector2.right * RayDistance);
		}
	}
}

