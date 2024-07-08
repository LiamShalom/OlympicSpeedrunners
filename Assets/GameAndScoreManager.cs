
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAndScoreManager : MonoBehaviour
{
   

    public List<PlayerController> AlivePlayers;

    BoxCollider2D borders;
    BoxCollider2D player; 
   
    public TextMesh MidMatchScoreDisplay;
    
    
  
    // Start is called before the first frame update
    void Start()
    {
        
    }



    void Update()
    {
        borders = gameObject.GetComponent<BoxCollider2D>();
        //OnTriggerEnter2D(borders);



    }
}


    /*
     * Border logic and midgame logic
     *

    void OnTriggerEnter2D(Collider2D other)
    {
        
        for (int i = 0; i < AlivePlayers.Count; i++)
        {
           // player = AlivePlayers[i].GetComponent<BoxCollider2D>();
            if (other.CompareTag("Player") )
            {
                AlivePlayers.RemoveAt(i);
                AlivePlayers[i].isAlive == false;
                //add 

            }
        }
    }

  

   PlayerController IsLastOneAlive()
   {
        if (AlivePlayers.Count == 1)
        {
            AlivePlayers[0].NumberOfWins++; 
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

   }

    bool HasThreeWins()
    {
        for (int i = 0; i < AlivePlayers.Count; i++)
            if (AlivePlayers[i].NumberOfWins == 3)
            {
                return true;
            }
    }


}
    */
