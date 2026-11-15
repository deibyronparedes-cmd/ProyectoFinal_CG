using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private bool isDead = false;

    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe un GameManager activo.");
            return;
        }

        GameManager.Instance.ApplyDamage(amount);

        // Comprobar si el jugador ha llegado a cero
        if (GameManager.Instance.CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // Detener el movimiento
        PlayerController movement =
            GetComponent<PlayerController>();

        if (movement != null)
            movement.enabled = false;

        // Detener las interacciones
        InteractionController interaction =
            GetComponent<InteractionController>();

        if (interaction != null)
            interaction.enabled = false;

        Debug.Log("El jugador ha sido derrotado.");
    }

    // Metodo temporal para probar el sistema
    [ContextMenu("PRUEBA: Recibir 20 de daño")]
    private void TestDamage()
    {
        TakeDamage(20);
    }
}
