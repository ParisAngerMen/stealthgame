using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public Slider slider;
    [SerializeField] private float maxHealth = 100f;
    private float curHealth;
    
    
    void Start()
    {
        slider.maxValue = maxHealth;
        curHealth = maxHealth;
    }

    private void Update()
    {
        Debug.Log("Health: " + curHealth);
        slider.value = curHealth;
    }

    public void HealPlayer(float amount)
    {
        curHealth += amount;
    }
    
    public void TakeDamage(float damage)
    {
        curHealth -= damage;
        if (curHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Death logic
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


}
