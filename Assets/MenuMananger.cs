using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    public GameObject PauseMenu;
    public GameObject MainMenu;
    public GameObject SettingMenu;

    

    //the purpose of knowing where the origin of the setting menu is from is so that you are able to
    // return to the correct screen when pressing esc
    public bool MainMenuShown = false;
    public bool SettingMenuFromMainMenuShown = false;
    public bool SettingMenuFromPauseMenuShown = false;
    public bool PauseMenuShown = false;


     
    
    // Start is called before the first frame update
    void Start()
    {
        DisplayMainMenu(); 
        
    }


    void Update()
    {
        

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingMenuFromMainMenuShown == true)
            {
                SettingMenu.SetActive(false);
                SettingMenuFromMainMenuShown = false;
                DisplayMainMenu();
            }else if(SettingMenuFromPauseMenuShown == true)
            {
                SettingMenu.SetActive(false);
                SettingMenuFromMainMenuShown = false;
                DisplayPauseMenu();
            }    
            else if (PauseMenuShown == true)
            {
                AllMenusOff(); 
            }else if (MainMenuShown == false)
                DisplayPauseMenu(); 
        }
        
    }

    // Pause Menu options

    public void ResumeGame()
    {

        PauseMenu.SetActive(false);
        Time.timeScale = 1f;
        
        
    }

    public void MainMenuSettings()
    {
        MainMenu.SetActive(false);
        SettingMenu.SetActive(true);
        SettingMenuFromMainMenuShown = true; 
    }

    public void PauseMenuSettings()
    { 
        PauseMenu.SetActive(false);
        SettingMenu.SetActive(true);
        SettingMenuFromPauseMenuShown = true;
    }

    public void Quit()
    {
        PauseMenu.SetActive(false);
        ResetTheGame();
        MainMenu.SetActive(true);

    }

    // Main Menu options

    public void SinglePlayer()
    {
        AllMenusOff();
        Time.timeScale = 1f;
         
    }


    //Setting Menu options

    public void back()
    {
        if (SettingMenuFromMainMenuShown == true)
        {
            SettingMenu.SetActive(false);
            SettingMenuFromMainMenuShown = false; 
            DisplayMainMenu();
        }
        if (SettingMenuFromPauseMenuShown == true)
        {
            SettingMenu.SetActive(false);
            SettingMenuFromPauseMenuShown = false;
            DisplayPauseMenu();
        }
    }

    // Helper Methods

    public void DisplayMainMenu()
    {

        MainMenu.SetActive(true);
        Time.timeScale = 0f;
        MainMenuShown = true;
        

    } 
    

    void DisplayPauseMenu()
    {

        PauseMenu.SetActive(true);
        Time.timeScale = 0f;
        PauseMenuShown = true;
        
    }

    
    void ResetTheGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    void AllMenusOff()
    {
        SettingMenu.SetActive(false);
        MainMenu.SetActive(false);
        PauseMenu.SetActive(false);
        MainMenuShown = false;
        SettingMenuFromMainMenuShown = false;
        SettingMenuFromPauseMenuShown = false;
        PauseMenuShown = false;
}


    
   
}
