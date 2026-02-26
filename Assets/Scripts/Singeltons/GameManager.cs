using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Instancia estática para acceder desde cualquier script: GameManager.Instance
    public static GameManager Instance { get; private set; }

    public int countPalanca = 0;

    private void Awake()
    {
        // Lógica para que sea un Singleton real y no se destruya entre escenas
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void DeathHuman()
    {
        GameObject[] humans = GameObject.FindGameObjectsWithTag("Human");
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");

        // Si solo queda 1 humano y 1 monstruo (final del juego)
        if (humans.Length == 1 && monsters.Length == 1)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        // Eliminar al humano actual
        foreach (GameObject h in humans)
        {
            Destroy(h);
        }

        // Convertir la lista de objetos en una lista de componentes PlayerMovement
        List<PlayerMovement> monsterScripts = new List<PlayerMovement>();
        foreach (GameObject m in monsters)
        {
            monsterScripts.Add(m.GetComponent<PlayerMovement>());
        }

        SelectHuman(monsterScripts);
    }

    public void UpdatePalanca()
    {
        countPalanca++;

        if (countPalanca == 4)
        {
            StartCoroutine(StunAllMonstersRoutine());
        }
    }

    private IEnumerator StunAllMonstersRoutine()
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        
        foreach (GameObject m in monsters)
        {
            PlayerMovement playerScript = m.GetComponent<PlayerMovement>();
            if (playerScript != null)
            {
                // Aquí llamarías al método que traducimos antes
                // playerScript.StartStun(); 
            }
            yield return new WaitForSeconds(0.2f);
        }

        countPalanca = 0;

        // Resetear palancas
        GameObject[] palancas = GameObject.FindGameObjectsWithTag("Palanca");
        foreach (GameObject p in palancas)
        {
            // Asumiendo que tienes un script Palanca con la variable canActive
            // p.GetComponent<Palanca>().canActive = true;
        }
    }

    public void SelectHuman(List<PlayerMovement> listPlayers)
    {
        if (listPlayers.Count == 0) return;

        int nRandom = Random.Range(0, listPlayers.Count);

        for (int i = 0; i < listPlayers.Count; i++)
        {
            if (i == nRandom)
            {
                listPlayers[i].SetAsHuman();
            }
            else
            {
                listPlayers[i].SetAsMonster();
            }
        }
    }
}
