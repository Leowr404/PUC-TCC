using UnityEngine;

public class PrinterWeakPoint : MonoBehaviour
{
    [Header("Referencia ao Boss")]
    public PrinterBoss printerBoss;

    void Start()
    {
        if (printerBoss == null)
        {
            // Tenta achar o componente PrinterBoss automaticamente no objeto pai caso esqueça de arrastar
            printerBoss = GetComponentInParent<PrinterBoss>();
        }
    }

    // Método chamado pelo LaserPointer quando o laser acerta este objeto
    public void HitByLaser()
    {
        if (printerBoss != null)
        {
            printerBoss.OnHitByLaserWeakPoint();
        }
    }
}