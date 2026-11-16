using UnityEngine;

// Movimento ciclico leggero per le figure: oscillazione di rotazione e/o dondolio verticale.
public class Oscilla : MonoBehaviour
{
    public Vector3 asse = Vector3.right;   // asse di rotazione (locale)
    public float ampiezza = 10f;           // gradi
    public float velocita = 1f;            // radianti al secondo
    public float fase;
    public float su;                       // ampiezza del dondolio verticale (unità)

    Quaternion rot0; Vector3 pos0;

    void Awake() { rot0 = transform.localRotation; pos0 = transform.localPosition; }

    void Update()
    {
        float s = Mathf.Sin(Time.time * velocita + fase);
        transform.localRotation = rot0 * Quaternion.AngleAxis(ampiezza * s, asse);
        if (su != 0f) transform.localPosition = pos0 + Vector3.up * (su * s);
    }
}
