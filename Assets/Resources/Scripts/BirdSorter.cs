using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class BirdSorter : MonoBehaviour
{
    float birdPlace;
    int t; // temporary variable
    int countRows, levelNumber;
    Vector2 ray;
    RaycastHit2D hit;
    GameObject tempRow, hitRow, tempBird;
    List<GameObject> allBirds, currentBirds, tempBirds;
    Dictionary<GameObject, List<GameObject>> dictRows, startDictRows;
    Dictionary<string, GameObject> allMyGameObjects;
    public GameObject[] birdTypes;
    public GameObject birdRow, victoryText, nextLevelButton, levelCounter;
    string fileLevel, fileSave;
    string[] content;


    void Start()
    {
        Application.targetFrameRate = 60;

        fileLevel = Path.Combine(Application.persistentDataPath, "levels.xml");
        fileSave = Path.Combine(Application.persistentDataPath, "save.xml");

        // Checking if file exists
        if (!File.Exists(fileLevel))
        {
            File.WriteAllText(fileLevel, "1 4");
            File.WriteAllText(fileSave, "");
        }

        // Reading file with levels
        content = File.ReadAllText(fileLevel).Trim().Split(" ");
    
        // Trying to read integer
        if (!int.TryParse(content[0], out levelNumber))
        {
            Debug.Log("ERROR: File does not contain a number.");
            return;
        }
        if (!int.TryParse(content[1], out countRows))
        {
            Debug.Log("ERROR: File does not contain a number.");
            return;
        }

        levelCounter.GetComponent<Text>().text = $"LEVEL {levelNumber}";

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


    //////////////////////////////////////////////////////////////////////////
    //////////                  Game Started Here                   //////////
    //////////////////////////////////////////////////////////////////////////


    void Update()
    {
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            ray = Camera.main.ScreenToWorldPoint(Input.touches[0].position);
            hit = Physics2D.Raycast(ray, Vector3.forward);
            
            if (currentBirds.Count == 0 && hit.transform.CompareTag("Row"))
            {
                // if there is birds, select all the similar in a row
                tempRow = hit.transform.gameObject;
                t = dictRows[tempRow].Count;
                if (t > 0)
                    currentBirds.Add(dictRows[tempRow][t-1]);
                for (int i = t - 1; i > 0; i--)
                    if (dictRows[hit.transform.gameObject].Count > 0)
                    {
                        if (dictRows[tempRow][i].tag == dictRows[tempRow][i-1].tag)
                            currentBirds.Add(dictRows[tempRow][i-1]);
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

                    if (t < 4 && hitRow != tempRow && (t == 0 || dictRows[hitRow][t-1].tag == v.tag))
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
                RestartBirds();
            }
            else if (hit.transform.CompareTag("NextLevel"))
            {
                StopAllCoroutines();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else if (currentBirds.Count > 0)
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
        Vector3 pos = new Vector3(0.2f + hit.transform.position.x + birdPlace * (-2 + numBirds), hit.transform.position.y + 0.3f, 0);
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
                        v.transform.position = new Vector3(0.3f + k.transform.position.x + birdPlace * (-2 + t++), k.transform.position.y + 0.3f, 0);
                    }
                }
            }
        }
    }

    void CreateLevel()
    {   
        /*
        for (int i = 0; i < countRows; i++)
        {
            tempRow = Instantiate(birdRow, new Vector3(-0.4f, 3.2f - i, 0), transform.rotation);
            tempRow.name = "Row" + i;
            dictRows.Add(tempRow, new List<GameObject>());
            startDictRows.Add(tempRow, new List<GameObject>());
            if (i < countRows - 2)
            {
                allMyGameObjects.Add(tempRow.name, tempRow);
                for (int j = 0; j < 4; j++)
                {
                    // creating temporary birds list for current row, then deleting used birds from first list
                    t = Random.Range(0, allBirds.Count);
                    tempBirds.Add(allBirds[t]);
                    allBirds.RemoveAt(t);
                }
                foreach (var item in tempBirds)
                {
                    dictRows[tempRow].Add(item);
                    startDictRows[tempRow].Add(item);
                }
                tempBirds.Clear();
            }
        }
        
        birdPlace = tempRow.transform.localScale.x / 4;
        
        File.WriteAllText(fileSave, "");
        StreamWriter fileStream = new StreamWriter(fileSave, true);
        foreach (var (k, l) in dictRows)
        {
            if (l.Count > 0)
            {
                foreach (var v in l)
                    fileStream.Write($"{v.name} ");
                fileStream.WriteLine();
            }
        }
        fileStream.Close();
        */

        File.WriteAllText(fileSave, "");
        StreamWriter fileStream = new StreamWriter(fileSave, true);
        List<string> strings = new List<string>();

        for (int i = 0; i < 4 * (countRows - 2); i++)
            strings.Add("Bird" + i);
        
        for (int i = 0; i < countRows - 2; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                t = Random.Range(0, strings.Count);
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
            tempRow = Instantiate(birdRow, new Vector3(-0.4f, 3.2f - i, 0), transform.rotation);
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
                    v.transform.position = new Vector3(0.3f + k.transform.position.x + birdPlace * (-2 + t++), k.transform.position.y + 0.3f, 0);
            }
    }

    void Victory()
    {
        if (levelNumber < 3)
            countRows = 5;
        else if (levelNumber < 10)
            countRows = Random.Range(6, 8);
        else
            countRows = Random.Range(7, 10);
        File.WriteAllText(fileLevel, $"{++levelNumber} {countRows}");
        CreateLevel();
        Instantiate(victoryText);
        Instantiate(nextLevelButton);
    }
}
