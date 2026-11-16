using UnityEngine;

// Camera che orbita attorno a un punto. Mouse: trascina per ruotare, rotella per zoom.
// Telefono: un dito ruota, due dita avvicinano/allontanano.
// Un tocco breve senza trascinare è un "clic" (lo legge Esplora).
public class CameraOrbita : MonoBehaviour
{
    public Vector3 bersaglio = Vector3.zero;
    public float distanza = 150f, yaw = 30f, pitch = 55f;
    public float distMin = 8f, distMax = 260f;

    Vector3 bersaglioMorbido;
    float distMorbida;
    Vector2 inizio;
    float tempoInizio;
    bool trascinato;

    public bool ClicEseguito { get; private set; }
    public Vector2 PosizioneClic { get; private set; }

    void Start() { bersaglioMorbido = bersaglio; distMorbida = distanza; }

    public void Vai(Vector3 punto, float dist)
    {
        bersaglio = punto;
        distanza = Mathf.Clamp(dist, distMin, distMax);
    }

    void Update()
    {
        ClicEseguito = false;

        if (Input.touchCount >= 2)
        {
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            float prima = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
            float ora = (a.position - b.position).magnitude;
            if (prima > 1f) distanza *= prima / Mathf.Max(ora, 1f);
            trascinato = true;
        }
        else if (Input.touchCount == 1 || Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            Vector2 pos; Vector2 delta; bool giu, su;
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                pos = t.position; delta = t.deltaPosition;
                giu = t.phase == TouchPhase.Began;
                su = t.phase == TouchPhase.Ended;
            }
            else
            {
                pos = Input.mousePosition;
                delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 8f;
                giu = Input.GetMouseButtonDown(0);
                su = Input.GetMouseButtonUp(0);
            }

            if (giu) { inizio = pos; tempoInizio = Time.unscaledTime; trascinato = false; }
            else if (!su)
            {
                if ((pos - inizio).magnitude > 12f) trascinato = true;
                if (trascinato) { yaw += delta.x * 0.25f; pitch = Mathf.Clamp(pitch - delta.y * 0.25f, -80f, 85f); }
            }
            if (su && !trascinato && Time.unscaledTime - tempoInizio < 0.5f)
            {
                ClicEseguito = true; PosizioneClic = pos;
            }
        }

        float rotella = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rotella) > 0.01f) distanza *= 1f - rotella * 0.08f;
        distanza = Mathf.Clamp(distanza, distMin, distMax);
    }

    void LateUpdate()
    {
        float k = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
        bersaglioMorbido = Vector3.Lerp(bersaglioMorbido, bersaglio, k);
        distMorbida = Mathf.Lerp(distMorbida, distanza, k);
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = bersaglioMorbido - rot * Vector3.forward * distMorbida;
        transform.rotation = rot;
    }
}
