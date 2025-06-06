using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Advertisements;

public class BirdSorter : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    float birdPlace;
    int t; // temporary variable
    int countRows, levelNumber, musicOn;
    bool isAdLoaded = false;
    Vector2 ray;
    RaycastHit2D hit;
    GameObject tempRow, hitRow, tempBird;
    List<GameObject> allBirds, currentBirds, tempBirds;
    Dictionary<GameObject, List<GameObject>> dictRows, startDictRows;
    Dictionary<string, GameObject> allMyGameObjects;
    public GameObject[] birdTypes;
    public GameObject birdRow, victoryText, nextLevelButton, levelCounter, restartButton, square, circle, soundMenu;
    string fileLevel, fileSave;
    string[] content;
    private AudioSource soundPlayer;
    public AudioClip backTheme, victoryTheme, birdTapSound, UiTapSound;
    private string _gameId, _adUnitId;
    private string _androidAdUnitId = "Interstitial_Android";
    private GameObject victoryWindow;



    //////////////////////////////////////////////////////////////////////////
    //////////                  Ads config start here               //////////
    //////////////////////////////////////////////////////////////////////////



    public void InitializeAds()
    {
        _gameId = "5857177";

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(_gameId, true, this);
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
    }
 
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }

    public void LoadAd()
    {
        Debug.Log("Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void ShowAd()
    {
        // Note that if the ad content wasn't previously loaded, this method will fail
        Debug.Log("Showing Ad: " + _adUnitId);
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad loaded: " + _adUnitId);
        isAdLoaded = true;
        soundPlayer.gameObject.GetComponent<SoundObject>().adsCount = 1;
    }

    public void OnUnityAdsFailedToLoad(string _adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Ad Unit: {_adUnitId} - {error.ToString()} - {message}");
        // Optionally execute code if the Ad Unit fails to load, such as attempting to try again.
    }

    public void OnUnityAdsShowFailure(string _adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Ad Unit {_adUnitId}: {error.ToString()} - {message}");
        // Optionally execute code if the Ad Unit fails to show, such as loading another ad.
    }
 
    public void OnUnityAdsShowStart(string _adUnitId) { }
    public void OnUnityAdsShowClick(string _adUnitId) { }
    public void OnUnityAdsShowComplete(string _adUnitId, UnityAdsShowCompletionState showCompletionState) { }



    //////////////////////////////////////////////////////////////////////////
    //////////                  Ads config end here                 //////////
    //////////////////////////////////////////////////////////////////////////



    void Awake()
    {
        Application.targetFrameRate = 60;

        _adUnitId = _androidAdUnitId;

        InitializeAds();

        fileLevel = Path.Combine(Application.persistentDataPath, "levels.xml");
        fileSave = Path.Combine(Application.persistentDataPath, "save.xml");

        // Checking if file exists
        if (!File.Exists(fileLevel))
        {
            File.WriteAllText(fileLevel, "1 4 1");
            File.WriteAllText(fileSave, "");
        }

        // Reading file with levels
        content = File.ReadAllText(fileLevel).Trim().Split(" ");

        // Trying to read integer
        if (!int.TryParse(content[0], out levelNumber))
            return;
        if (!int.TryParse(content[1], out countRows))
            return;
        if (content.Length > 2 && !int.TryParse(content[2], out musicOn))
            return;

        levelCounter.GetComponent<Text>().text = $"LEVEL {levelNumber}";
        UnityEngine.Random.InitState(levelNumber);

        allBirds = new List<GameObject>();
        tempBirds = new List<GameObject>();
        dictRows = new Dictionary<GameObject, List<GameObject>>();
        startDictRows = new Dictionary<GameObject, List<GameObject>>();
        currentBirds = new List<GameObject>();
        allMyGameObjects = new Dictionary<string, GameObject>();

        for (int i = 0; i < countRows - 2; i++)
            for (int j = 0; j < 4; j++)
            {
                // creating list with all the birds used at this currently game
                tempBird = Instantiate(birdTypes[i]);
                tempBird.name = "Bird" + ((countRows - 2) * j + i);
                allBirds.Add(tempBird);
                allMyGameObjects.Add(tempBird.name, tempBird);
            }

        if (levelNumber == 1)
            CreateLevel();
        ReadLevel();
        PlaceBirds();
    }

    void Start()
    {
        if (GameObject.FindWithTag("Sound") is null)
        {
            soundPlayer = Instantiate(soundMenu).GetComponent<AudioSource>();
            if (musicOn == 1)
            {
                soundPlayer.clip = backTheme;
                soundPlayer.Play();
            }
            else
                musicOn = 0;
        }
        else
            soundPlayer = GameObject.FindWithTag("Sound").GetComponent<AudioSource>();

        StartCoroutine(CircleHide(true));
    }


    //////////////////////////////////////////////////////////////////////////
    //////////                  Game Started Here                   //////////
    //////////////////////////////////////////////////////////////////////////


    void Update()
    {
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            ray = Camera.main.ScreenToWorldPoint(Input.touches[0].position);
            hit = Physics2D.Raycast(ray, Vector3.forward);

            if (hit)
            {
                if (currentBirds.Count == 0 && hit.transform.CompareTag("Row"))
                {
                    // if there is birds, select all the similar in a row
                    tempRow = hit.transform.gameObject;
                    t = dictRows[tempRow].Count;
                    if (t > 0)
                        currentBirds.Add(dictRows[tempRow][t - 1]);
                    for (int i = t - 1; i > 0; i--)
                        if (dictRows[hit.transform.gameObject].Count > 0)
                        {
                            if (dictRows[tempRow][i].tag == dictRows[tempRow][i - 1].tag)
                                currentBirds.Add(dictRows[tempRow][i - 1]);
                            else
                                break;
                        }

                    foreach (var v in currentBirds)
                    {
                        v.transform.GetComponent<SpriteRenderer>().color = Color.green;
                    }
                }
                else if (currentBirds.Count > 0 && hit.transform.CompareTag("Row"))
                {
                    hitRow = hit.transform.gameObject;
                    t = dictRows[hitRow].Count;

                    foreach (var v in currentBirds)
                    {
                        v.transform.GetComponent<SpriteRenderer>().color = Color.white;

                        if (t < 4 && hitRow != tempRow && (t == 0 || dictRows[hitRow][t - 1].tag == v.tag))
                        {
                            dictRows[hitRow].Add(v);
                            dictRows[tempRow].Remove(v);
                            StartCoroutine(BirdFly(hitRow, v));
                            t++;
                        }
                    }
                    currentBirds.Clear();

                    if (dictRows[hitRow].Count == 4 && dictRows[hitRow][0].tag == dictRows[hitRow][1].tag
                                                    && dictRows[hitRow][1].tag == dictRows[hitRow][2].tag
                                                    && dictRows[hitRow][2].tag == dictRows[hitRow][3].tag)
                    {
                        countRows--;
                        hitRow.GetComponent<BoxCollider2D>().enabled = false;
                    }
                    if (countRows == 2)
                        Victory();
                }
                else if (hit.transform.CompareTag("Restart"))
                {
                    StopAllCoroutines();
                    foreach (var v in currentBirds)
                        v.transform.GetComponent<SpriteRenderer>().color = Color.white;
                    currentBirds.Clear();
                    soundPlayer.PlayOneShot(UiTapSound, 0.6f * musicOn);
                    RestartBirds();
                }
                else if (hit.transform.CompareTag("NextLevel"))
                    StartCoroutine(CircleHide(false));

                else if (hit.transform.CompareTag("Menu"))
                    SetMusicSettings();
            }
            else
            {
                foreach (var v in currentBirds)
                    v.transform.GetComponent<SpriteRenderer>().color = Color.white;
                currentBirds.Clear();
            }
        }
    }


    //////////////////////////////////////////////////////////////////////////
    //////////                  Game Ended Here                     //////////
    //////////////////////////////////////////////////////////////////////////


    IEnumerator BirdFly(GameObject hit, GameObject curBird)
    {
        int numBirds = dictRows[hit].Count - 1;
        Vector3 pos = new Vector3(0.2f + hit.transform.position.x + birdPlace * (-2 + numBirds), hit.transform.position.y + 0.15f, 0);
        while (Vector3.Distance(pos, curBird.transform.position) > 0.1f)
        {
            curBird.transform.Translate(new Vector3(pos.x - curBird.transform.position.x, pos.y - curBird.transform.position.y, 0).normalized * Time.deltaTime * 5);
            yield return null;
        }
        curBird.transform.position = pos;
    }

    void RestartBirds()
    {
        if (countRows > 2)
        {
            countRows = startDictRows.Count;
            dictRows.Clear();
            foreach (var (k, l) in startDictRows)
            {
                k.GetComponent<BoxCollider2D>().enabled = true;
                dictRows.Add(k, new List<GameObject>());
                if (l.Count > 0)
                {
                    t = 0;
                    foreach (var v in l)
                    {
                        dictRows[k].Add(v);
                        v.transform.position = new Vector3(0.2f + k.transform.position.x + birdPlace * (-2 + t++), k.transform.position.y + 0.15f, 0);
                    }
                }
            }
        }
    }

    void CreateLevel()
    {   
        StreamWriter fileStream = new StreamWriter(fileSave, false);
        List<string> strings = new List<string>();

        for (int i = 0; i < 4 * (countRows - 2); i++)
            strings.Add("Bird" + i);
        
        for (int i = 0; i < countRows - 2; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                t = UnityEngine.Random.Range(0, strings.Count);
                fileStream.Write($"{strings[t]} ");
                strings.RemoveAt(t);
            }
            fileStream.WriteLine();
        }
        fileStream.Close();
    }

    void ReadLevel()
    {
        StreamReader fileStream = new StreamReader(fileSave);
        for (int i = 0; i < countRows; i++)
        {
            tempRow = Instantiate(birdRow, new Vector3(-0.3f, 3.2f - i * 0.9f, 0), transform.rotation);
            tempRow.name = "Row" + i;
            dictRows.Add(tempRow, new List<GameObject>());
            startDictRows.Add(tempRow, new List<GameObject>());
            if (i < countRows - 2)
            {
                allMyGameObjects.Add(tempRow.name, tempRow);
                string line = fileStream.ReadLine();

                // creating temporary birds list for current row, reading from fileSave
                content = line.Trim().Split(" ");
                foreach (string s in content)
                    tempBirds.Add(allMyGameObjects[s]);
                foreach (var item in tempBirds)
                {
                    dictRows[tempRow].Add(item);
                    startDictRows[tempRow].Add(item);
                }
                tempBirds.Clear();
            }
        }
        fileStream.Close();
        birdPlace = tempRow.transform.localScale.x / 4;
    }

    void PlaceBirds()
    {
        foreach (var (k, l) in dictRows)
            if (l.Count > 0)
            {
                t = 0;
                foreach (var v in l)
                    v.transform.position = new Vector3(0.2f + k.transform.position.x + birdPlace * (-2 + t++), k.transform.position.y + 0.15f, 0);
            }
    }

    void SetMusicSettings()
    {
        if (musicOn == 0)
        {
            musicOn = 1;
            soundPlayer.clip = backTheme;
            soundPlayer.Play();
        }
        else
        {
            musicOn = 0;
            soundPlayer.clip = null;
        }
        // Saving music settings
        File.WriteAllText(fileLevel, $"{levelNumber} {countRows} {musicOn}");
    }

    IEnumerator VictoryEffect()
    {
        SpriteRenderer alpha = square.GetComponent<SpriteRenderer>();
        for (float i = 0; i < 0.5f; i += 0.05f)
        {
            alpha.color = new Color(0, 0.2f, 0.1f, i);
            yield return null;
        }
        victoryWindow = Instantiate(victoryText);
        victoryWindow.transform.localScale = new Vector3(0, 0, 0);

        for (float i = 0; i < 0.55f; i += 0.05f)
        {
            victoryWindow.transform.localScale = new Vector3(i, i, i);
            yield return null;
        }
        for (float i = 0.55f; i > 0.4f; i -= 0.02f)
        {
            victoryWindow.transform.localScale = new Vector3(i, i, i);
            yield return null;
        }
        /*if (isAdLoaded)
            ShowAd();*/
    }

    IEnumerator CircleHide(bool type)
    {
        if (type)
        {
            for (float i = 1; i > 0; i -= 0.1f)
            {
                circle.GetComponent<Image>().fillAmount = i;
                yield return null;
            }
            circle.GetComponent<Image>().fillAmount = 0;
            
            if (soundPlayer.gameObject.GetComponent<SoundObject>().adsCount == 3)
                LoadAd();
            else
                soundPlayer.gameObject.GetComponent<SoundObject>().adsCount += 1;
        }
        else
        {
            for (float i = 0.56f; i > 0; i -= 0.02f)
            {
                victoryWindow.transform.localScale = new Vector3(i, i, i);
                yield return null;
            }
            victoryWindow.transform.localScale = new Vector3(0, 0, 0);
            for (float i = 0; i <= 1; i += 0.1f)
            {
                circle.GetComponent<Image>().fillAmount = i;
                yield return null;
            }
            circle.GetComponent<Image>().fillAmount = 1;
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void Victory()
    {
        restartButton.GetComponent<BoxCollider2D>().enabled = false;
        soundPlayer.PlayOneShot(victoryTheme, musicOn);
        if (levelNumber < 3)
            countRows = 5;
        else if (levelNumber < 10)
            countRows = UnityEngine.Random.Range(6, 8);
        else
            countRows = UnityEngine.Random.Range(7, 11);
        File.WriteAllText(fileLevel, $"{++levelNumber} {countRows} {musicOn}");
        CreateLevel();
        StartCoroutine(VictoryEffect());
    }
}
