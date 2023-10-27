using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public Material waveMaterial;
    public Texture2D waveTexture;
    public bool reflectiveBoundary;
    float[][] waveN, waveNm1, waveNp1; //informacion de estado waveN es el estaro actual,wnm es menos 1 (o estado previo) y wp1 es estado sigueinte
    float Lx = 10;//ancho
    float Ly = 10;//alto
    [SerializeField] float dx = 0.1f; //densidad del eje x
    float dy { get => dx; } // densidad del eje y
    int nx, ny; // resolucion
    //variables para ecuacion de onda
    public float CFL = 0.05f; 
    public float c = 1;//la velocidad de propagacion
    float dt; //cambio en el tiempo
    float t; //tiempo actual
    [SerializeField] float floatToColorMultiplier=2f; //para cambiar directamente la enfatizacion del color
    [SerializeField] float pulseFrecuency = 1f;
    [SerializeField] float pulseMagnitude = 1f;
    [SerializeField] Vector2Int pulsePosition = new Vector2Int(50,50);
    [SerializeField] float elasticity = 0.98f; //determina cuanto pasara antes de que la onda se discipe 
    // Start is called before the first frame update
    void Start()
    {

        nx = Mathf.FloorToInt(Lx / dx);
        ny = Mathf.FloorToInt(Ly / dy);
        waveTexture = new Texture2D(nx, ny, TextureFormat.RGBA32, false);
        waveN = new float[nx][];
        waveNm1 = new float[nx][];
        waveNp1 = new float[nx][];
        //inicializacion de las variables de informacion de estado
        for (int i = 0; i < nx; i++)
        {
            waveN[i] = new float[ny];
            waveNm1[i] = new float[ny];
            waveNp1[i] = new float[ny];
        }
        waveMaterial.SetTexture("_MainTex", waveTexture);//coloring texture
        waveMaterial.SetTexture("_Displacement", waveTexture);//displacement texture




    }
    void WaveStep()
    {

        dt = CFL * dx / c; //recalculamos dt
        t += dx; //incrementamos el tiempo
        if (reflectiveBoundary)
        {
            ApplyReflectiveBoundary();
        }
        else
        {
            ApplyAbsortiveBoundary();
        }
        //copia el contenido del estado de onda anterior al estado de onda actual, i es el estado anterior y j es el actual
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                waveNm1[i][j] = waveN[i][j]; // copia el estado en N a N-1
                waveN[i][j] = waveNp1[i][j];// copia el es estado N+1 A N 
            }
        }
        //agrega onda a la malla para que no este estatica 
        waveN[pulsePosition.x][pulsePosition.y] = dt * dt * 20 * 20 *pulseMagnitude* Mathf.Cos(t * Mathf.Rad2Deg*pulseFrecuency);
        for (int i = 1; i < nx - 1; i++)
        { //no procesa las esquinas de la textura    
            for (int j = 1; j < ny - 1; j++)
            {
                float n_ij = waveN[i][j];
                float n_ip1j = waveN[i + 1][j];
                float n_im1j = waveN[i - 1][j];
                float n_ijp1 = waveN[i][j + 1];
                float n_ijm1 = waveN[i][j - 1];
                float nm1_ij = waveNm1[i][j];
                waveNp1[i][j] = 2f * n_ij - nm1_ij + CFL * CFL * (n_ijm1 + n_ijp1 + n_im1j + n_ip1j - 4f * n_ij);//ecuacion de onda
                waveNp1[i][j] *= elasticity; //para que la onda se discipe 
            }

        }
    }
    void ApplyMatrixToTexture(float[][] state, ref Texture2D tex,float floatToColorMultiplier)
    {
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                float val = state[i][j]* floatToColorMultiplier;
                tex.SetPixel(i, j, new Color(val+0.5f, val + 0.5f, val + 0.5f, 1f)); //pinta en escala de grises 

            }
            tex.Apply();
        }
    }
    void ApplyReflectiveBoundary()
    {
        //se utiliza para no cambiar los valores de las esquinas
        for (int i = 0; i < nx; i++)
        {
            waveN[i][0] = 0f;
            waveN[i][ny - 1] = 0f;


        }
        for (int j = 0; j < ny; j++)
        {
            waveN[0][j] = 0f;
            waveN[ny - 1][j] = 0f;



        }

    }
    void ApplyAbsortiveBoundary()//signifca que no absorbera las propagaciones y seguiran fuera de la textura
    {
        float v = (CFL - 1F) / (CFL + 1F);
        for (int i = 0; i < nx; i++)
        {
            waveNp1[i][0] = waveN[i][1]+v*(waveNp1[i][1]-waveN[i][0]);
            waveNp1[i][ny - 1] = waveN[i][ny -  2] + v * (waveNp1[i][ny - 2] - waveN[i][ny - 1]);


        }
        for (int j = 0; j < ny; j++)
        {
            waveNp1[0][j] = waveN[1][j] + v * (waveNp1[1][j] - waveN[0][j]);    
            waveNp1[ny - 1][j] = waveN[ny - 2][j] + v * (waveNp1[ny - 2][j] - waveN[ny - 1][j]); ;



        }
    }
    void FollowMouseOnTexture(ref Vector2Int pos){
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray,out hit)){
            pos = new Vector2Int((int)(hit.textureCoord.x * nx),(int)(hit.textureCoord.y * ny));
        }
    } 
    // Update is called once per frame
    void Update()
    {
        FollowMouseOnTexture(ref pulsePosition);
        WaveStep();
        ApplyMatrixToTexture(waveN, ref waveTexture, floatToColorMultiplier);
    }
}
