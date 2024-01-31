using System.Collections.Generic;
using UnityEngine;

namespace FirstPartyGames.BubbleShooter
{
    public class Shooter : MonoBehaviour
    {
        public bool canShoot;

        public float speed = 25f;
        [SerializeField]
        Transform nextBubblePosition;
        [SerializeField]
        GameObject currentBubble;
        [SerializeField]
        GameObject nextBubble;
        [SerializeField]
        GameObject bottomShootPoint;
        [SerializeField]
        LineRenderer lineRenderer;
        [SerializeField]
        LayerMask wall;
        [SerializeField]
        LayerMask bubble;
        [SerializeField]
        LayerMask topWall;
        private Vector2 lookDirection;
        private float lookAngle;
        private Vector2 gizmosPoint;

        public void Awake()
        {
            if (lineRenderer == null)
            {
                Debug.LogError("LineRenderer not assigned to Shooter script.");
            }

            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        public void Update()
        {
            if (GameManager.instance.gameState == "play")
            {
                OnDrawLine();
            }
        }

        public void Shoot()
        {
            if (currentBubble == null) CreateNextBubble();
            ScoreManager.GetInstance().AddThrows();
            AudioManager.instance.PlaySound("shoot");
            transform.rotation = Quaternion.Euler(0f, 0f, lookAngle - 90f);
            currentBubble.transform.rotation = transform.rotation;
            currentBubble.GetComponent<CircleCollider2D>().enabled = true;
            Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
            rb.AddForce(currentBubble.transform.up * speed, ForceMode2D.Impulse);
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale = 0;
            currentBubble = null;
        }

        public void SwapBubbles()
        {
            List<GameObject> bubblesInScene = LevelManager.instance.bubblesInScene;
            if (bubblesInScene.Count < 1) return;

            currentBubble.transform.position = nextBubblePosition.position;
            nextBubble.transform.position = transform.position;
            GameObject temp = currentBubble;
            currentBubble = nextBubble;
            nextBubble = temp;
        }

        public void CreateNewBubbles()
        {
            if (nextBubble != null)
                Destroy(nextBubble);

            if (currentBubble != null)
                Destroy(currentBubble);

            nextBubble = null;
            currentBubble = null;
            CreateNextBubble();
            canShoot = true;
        }

        public void CreateNextBubble()
        {
            List<GameObject> bubblesInScene = LevelManager.instance.bubblesInScene;
            List<string> colors = LevelManager.instance.colorsInScene;

            if (bubblesInScene.Count < 1) return;

            if (nextBubble == null)
            {
                nextBubble = InstantiateNewBubble(bubblesInScene);
            }
            else
            {
                // if (!colors.Contains(nextBubble.GetComponent<Bubble>().bubbleColor.ToString()))
                // {
                //     Destroy(nextBubble);
                //     nextBubble = InstantiateNewBubble(bubblesInScene);
                // }
            }

            if (currentBubble == null)
            {
                currentBubble = nextBubble;
                currentBubble.transform.position = transform.position;
                nextBubble = InstantiateNewBubble(bubblesInScene);
            }
        }

        private void OnDrawLine()
        {
            gizmosPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lookDirection = gizmosPoint - (Vector2)transform.position;
            lookAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, gizmosPoint);

            if (Input.GetMouseButton(0) && (Camera.main.ScreenToWorldPoint(Input.mousePosition).y > bottomShootPoint.transform.position.y))
            {
                if (LevelManager.instance != null && LevelManager.instance.GetBubbleAreaChildCount() > 0)
                {
                    lineRenderer.enabled = true;

                    RaycastHit2D hitBubble = Physics2D.Raycast(transform.position, lookDirection, Mathf.Infinity, bubble);
                    RaycastHit2D hitWall = Physics2D.Raycast(transform.position, lookDirection, Mathf.Infinity, wall);
                    RaycastHit2D hitop = Physics2D.Raycast(transform.position, lookDirection, Mathf.Infinity, topWall);

                    if (hitBubble.collider != null)
                    {
                        lineRenderer.positionCount = 2;
                        lineRenderer.SetPosition(1, hitBubble.point);
                    }
                    else if (hitWall.collider != null)
                    {
                        Vector2 reflectionDirection = Vector2.Reflect(lookDirection.normalized, hitWall.normal);
                        Vector2 reflectedWorldEndPoint = hitWall.point + reflectionDirection * 100f;

                        lineRenderer.positionCount = 4;
                        lineRenderer.SetPosition(2, hitWall.point);
                        lineRenderer.SetPosition(3, reflectedWorldEndPoint);
                        if (hitBubble.collider != null)
                        {
                            Debug.Log("Hit the bubbles");
                            lineRenderer.positionCount = 4;
                            lineRenderer.SetPosition(2, hitBubble.point);
                        }

                    }
                    else
                    {
                        lineRenderer.positionCount = 2;
                        lineRenderer.SetPosition(1, hitop.point);
                    }
                }
            }
            else
            {
                lineRenderer.enabled = false;
            }

            if (canShoot && Input.GetMouseButtonUp(0) && (Camera.main.ScreenToWorldPoint(Input.mousePosition).y > bottomShootPoint.transform.position.y))
            {
                canShoot = false;
                Shoot();
            }
        }



        private GameObject InstantiateNewBubble(List<GameObject> bubblesInScene)
        {
            if (bubblesInScene.Count > 0)
            {
                GameObject newBubble = Instantiate(bubblesInScene[Random.Range(0, bubblesInScene.Count)]);
                newBubble.transform.position = nextBubblePosition.position;
                newBubble.GetComponent<Bubble>().isFixed = false;
                newBubble.GetComponent<CircleCollider2D>().enabled = false;
                Rigidbody2D rb2d = newBubble.AddComponent(typeof(Rigidbody2D)) as Rigidbody2D;
                rb2d.gravityScale = 0f;
                return newBubble;
            }
            else
            {
                return null;
            }
        }
    }
}




