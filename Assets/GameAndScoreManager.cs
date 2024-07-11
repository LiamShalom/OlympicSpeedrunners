
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAndScoreManager : MonoBehaviour
{

     
    public List<PlayerController> Players;
    public GameObject leftBorder;
    public GameObject rightBorder;
    public GameObject topBorder;
    public GameObject bottomBorder;


    public GameObject MidMatchScoreDisplay;
    public CameraFollowingMultiplePlayers CameraDisplay; 
    
    
  
    // Start is called before the first frame update
    void Start()
    {
        leftBorder.SetActive(false);
        rightBorder.SetActive(false);
        topBorder.SetActive(false);
        bottomBorder.SetActive(false);


    }



    void Update()
    {
        
        OnTriggerEnter2D(leftBorder.GetComponent<BoxCollider2D>());
        OnTriggerEnter2D(rightBorder.GetComponent<BoxCollider2D>());
        OnTriggerEnter2D(topBorder.GetComponent<BoxCollider2D>());
        OnTriggerEnter2D(bottomBorder.GetComponent<BoxCollider2D>());
        IsLastOneAlive();


    }




    //Border logic and midgame logic
    

    void OnTriggerEnter2D(Collider2D other)
    {

        for (int i = 0; i < Players.Count; i++)
        {

            if (other.CompareTag("Player1"))
            {
                Players[0].isAlive = false;
                CameraDisplay.playerList.RemoveAt(0); 

            }
            else if (other.CompareTag("Player2"))
            {
                
                Players[1].isAlive = false;
                CameraDisplay.playerList.RemoveAt(1);
            }
            else if (other.CompareTag("Player3"))
            {
                Players[2].isAlive = false;
                CameraDisplay.playerList.RemoveAt(2);
            }
            else if (other.CompareTag("Player4"))
            {
                
                Players[3].isAlive = false;
                CameraDisplay.playerList.RemoveAt(3);
            }
        }
    }



    void IsLastOneAlive()
    {
        if (CameraDisplay.playerList.Count == 1)
        {
            for(int i = 0; i < Players.Count; i++)
            {
                if (Players[i].isAlive == true && HasThreeWins(Players[i]))
                {
                    DisplayFinalEndScreen(Players[i]);
                }
                else
                    Players[i].NumberOfWins++; 
                    
            }
            
        }
        else

            StartNewSubGame();
    }



    bool HasThreeWins(PlayerController player)
    {
        if (player.NumberOfWins == 3)
            return true;
        else
            return false; 
    }


    void StartNewSubGame() {
        // probably need to reset all the player isAlive variables. 
        MidMatchScoreDisplay.SetActive(true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ShrinkingBorder()
    {
        // should include checking if either 3 minutes has passed, or if a player has died. if either of these conditions are met then the border
        // will start shrinking. 
        
    }

    void DisplayFinalEndScreen(PlayerController WinnerPlayer)
    {
        //Make the final game over screen and

    }
    
}

