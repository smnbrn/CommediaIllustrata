using UnityEngine;

// Camera che orbita attorno a un punto. Mouse: trascina per ruotare, rotella per zoom.
// Telefono: un dito ruota, due dita avvicinano/allontanano.
// Tastiera: W A S D (o frecce) spostano la vista, E/Q salgono e scendono, Maiusc va più veloce.
// Un tocco breve senza trascinare è un "clic" (lo legge Esplora).
public class CameraOrbita : MonoBehaviour
{
    public Vector3 bersaglio = Vector3.zero;
    public float distanza = 150f, yaw = 30f, pitch = 55f;
    public float distMin = 8f, distMax = 260f;

    // Modalità automatica (viaggio cinematografico): la camera tende a questi valori.
    // Si spegne da sola se l'utente trascina o usa la rotella/pizzico.
    public bool automatica;
    public float yawTarget, pitchTarget = 30f, distTarget = 40f;
    public float morbidezza = 1.8f;
    public float scossa;   // piccolo scossone (1 = forte), si smorza da solo

    Vector3 bersaglioMorbido;
    float distMorbida;
    Vector2 inizio;
    float tempoInizio;
    bool trascinato;

    public bool Mosso { get; private set; }   // vero nel frame in cui la tastiera sposta la vista
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
        Mosso = false;

        if (Input.touchCount >= 2)
        {
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            float prima = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
            float ora = (a.position - b.position).magnitude;
            automatica = false;
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
                if (trascinato) { automatica = false; yaw += delta.x * 0.25f; pitch = Mathf.Clamp(pitch - delta.y * 0.25f, -80f, 85f); }
            }
            if (su && !trascinato && Time.unscaledTime - tempoInizio < 0.5f)
            {
                ClicEseguito = true; PosizioneClic = pos;
            }
        }

        float rotella = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rotella) > 0.01f) { automatica = false; distanza *= 1f - rotella * 0.08f; }
        distanza = Mathf.Clamp(distanza, distMin, distMax);

        // spostamento con la tastiera: orizzontale rispetto a dove guarda la camera, più veloce quando si è lontani
        float avanti = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
        float lato = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        float salita = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        if (avanti != 0f || lato != 0f || salita != 0f)
        {
            automatica = false; Mosso = true;
            Vector3 piano = Quaternion.Euler(0f, yaw, 0f) * new Vector3(lato, 0f, avanti);
            if (piano.sqrMagnitude > 1f) piano.Normalize();
            float v = Mathf.Max(distanza * 0.8f, 10f) * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 2.5f : 1f);
            bersaglio += (piano + Vector3.up * salita) * v * Time.unscaledDeltaTime;
        }
    }

    void LateUpdate()
    {
        if (automatica)
        {
            float ka = 1f - Mathf.Exp(-morbidezza * Time.unscaledDeltaTime);
            yaw = Mathf.LerpAngle(yaw, yawTarget, ka);
            pitch = Mathf.Lerp(pitch, pitchTarget, ka);
            distanza = Mathf.Lerp(distanza, distTarget, ka);
        }
        float k = 1f - Mathf.Exp(-(automatica ? 3.5f : 8f) * Time.unscaledDeltaTime);
        bersaglioMorbido = Vector3.Lerp(bersaglioMorbido, bersaglio, k);
        distMorbida = Mathf.Lerp(distMorbida, distanza, k);
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = bersaglioMorbido - rot * Vector3.forward * distMorbida;
        transform.rotation = rot;
        if (scossa > 0.001f)
        {
            transform.position += Random.insideUnitSphere * scossa * 0.6f;
            scossa = Mathf.MoveTowards(scossa, 0f, Time.unscaledDeltaTime * 1.4f);
        }
    }
}
