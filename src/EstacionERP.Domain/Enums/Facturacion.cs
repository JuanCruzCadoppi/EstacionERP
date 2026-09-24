namespace EstacionERP.Domain.Enums;

/// <summary>Tipos de comprobante. Valores = códigos ARCA (FEParamGetTiposCbte).</summary>
public enum TipoComprobante
{
    FacturaA = 1,
    NotaDebitoA = 2,
    NotaCreditoA = 3,
    FacturaB = 6,
    NotaDebitoB = 7,
    NotaCreditoB = 8,
    FacturaC = 11,
    NotaDebitoC = 12,
    NotaCreditoC = 13
}

/// <summary>Concepto del comprobante. Valores = códigos ARCA.</summary>
public enum ConceptoComprobante
{
    Productos = 1,
    Servicios = 2,
    ProductosYServicios = 3
}

public enum EstadoComprobante
{
    /// <summary>Guardado, esperando respuesta de ARCA (sin internet, error de conexión, etc.).</summary>
    Pendiente = 1,
    /// <summary>Con CAE: comprobante válido.</summary>
    Autorizado = 2,
    /// <summary>ARCA lo rechazó: no tiene validez fiscal.</summary>
    Rechazado = 3
}

public enum EntornoArca
{
    /// <summary>Servidores de prueba de ARCA: los comprobantes no tienen validez.</summary>
    Homologacion = 1,
    Produccion = 2
}
