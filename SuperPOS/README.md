fcffg# SuperPOS (WPF + ASP.NET Core + PostgreSQL)

SuperPOS es un sistema de punto de venta (POS) orientado a supermercados/retail, con cliente **WPF (Windows)** y una **API ASP.NET Core** respaldada por **PostgreSQL**.

Este README está pensado para **presentar avances** a cliente y **levantar requerimientos** (incluye muchas preguntas).

---

## Arquitectura (alto nivel)

- **Cliente**: `SuperPOS.Client` (WPF).
  - UI para caja, stock, compras, remitos, etc.
  - Se conecta a la API por HTTP (`ApiService`).
  - Configuración local en `appsettings.json` (ej: `ApiBaseUrl`).
- **API**: `SuperPOS.API` (ASP.NET Core).
  - Endpoints REST: artículos, remitos, ventas, OC, inventarios, reportes, etc.
  - Integración AFIP (WSAA/WSFE) para CAE cuando corresponde.
  - SignalR: `PosHub` para notificaciones (venta realizada).
- **Shared**: `SuperPOS.Shared` con entidades compartidas.
- **DB**: PostgreSQL + EF Core migrations (`SuperPOS.API/Migrations`).

---

## Cómo correr (demo rápida)

### API

- Proyecto: `SuperPOS/src/SuperPOS.API`
- Ejecutar migraciones (si corresponde) y levantar:

```bash
dotnet run --project "SuperPOS/src/SuperPOS.API/SuperPOS.API.csproj"
```

Healthcheck:
- `GET /api/health`

### Cliente WPF

- Proyecto: `SuperPOS/src/SuperPOS.Client`
- Configurar `appsettings.json`:
  - `ApiBaseUrl`: URL de API (ej: `http://localhost:5000/` o la que uses)
- Ejecutar:

```bash
dotnet run --project "SuperPOS/src/SuperPOS.Client/SuperPOS.Client.csproj"
```

---

## Funcionalidades / avances implementados (para mostrar hoy)

### Remitos (entrada/recepción)

- **Crear remito manual** desde el cliente.
- **Ver detalle de remito** desde la UI (detalle consumido como JSON para evitar ciclos).
- **Confirmar remito** en la API: guarda cantidades recibidas y actualiza stock del artículo.

### Ventas / Caja

- **Registrar venta** en API (comprobante + detalles).
- **Actualización de stock** al vender.
- **Anulación**: revierte stock.
- **Notificación en tiempo real** (SignalR) cuando se realiza una venta.

### AFIP (cuando el comprobante lo requiere)

- Flujo completo WSAA/WSFE para **solicitar CAE**, con manejo de fallos sin frenar la venta.
- Endpoints/servicios preparados para homologación/producción según config.

### Reportes / Dashboard

- Ajustes en métricas de “hoy” y “mes” (acotadas correctamente por fecha).

---

## Trazabilidad de productos (nuevo: base + MVP)

El cliente pidió trazabilidad “desde que llega al depósito, pasa por reposición y llega a caja”.

### Qué se implementó (base técnica)

Se agregó una tabla/eventos de trazabilidad:
- Entidad: `TrazabilidadEvento` (en `SuperPOS.Shared`)
- Tabla: `TrazabilidadEventos` (migration: `AddTrazabilidadEventos`)
- Endpoints:
  - `GET /api/trazabilidad/articulos/{idArticulo}`
  - `GET /api/trazabilidad/codigo/{codigoBarras}`
  - `POST /api/trazabilidad/eventos` (carga manual / reposición / merma, etc.)

### Automatismos actuales (MVP)

- **Recepción de remito confirmado**: genera eventos `RecepcionDeposito` por artículo recibido.
- **Venta**: genera eventos `VentaCaja` por cada detalle de comprobante.
- **Anulación**: genera eventos `AnulacionVenta` por detalle (reingreso).

### Qué falta (para trazabilidad “completa”)

- UI para que el **Repositor** registre “Reposición a sala” (scanner + ubicación + cantidad).
- Definir “ubicaciones” (depósito/sala/pasillo/góndola) y si se manejan como catálogo.
- Definir si se llevará trazabilidad por **lote/serie/vencimiento** obligatoria o solo opcional.
- Integración con `StockDeposito` vs `StockActual` (hoy el stock se mueve con `StockActual`; hay que acordar reglas de negocio).

---

## Qué demo conviene hacer en la presentación (guión)

- **Stock / artículos**: buscar por código de barras, ver datos principales.
- **Remitos**:
  - Crear remito manual (2–3 ítems).
  - Confirmarlo (recibir cantidades).
  - Mostrar que el stock cambia.
  - Abrir “Detalle remito” y ver líneas.
- **Caja / venta**:
  - Registrar una venta simple.
  - Mostrar descuento/IVA/total (si aplica en la UI actual).
  - Confirmar que stock baja.
- **Trazabilidad (concepto)**:
  - Mostrar endpoint `GET /api/trazabilidad/codigo/{ean}` y listar la línea temporal:
    - Recepción → Venta → (opcional) Anulación
  - Explicar el siguiente paso: Reposición (repositorio) con ubicación.

---

## Preguntas para el cliente (muy importantes)

La idea es cerrar estas respuestas hoy o dejar pendientes con prioridad.

### Operación real del negocio

- **¿Cuántas sucursales** va a manejar el sistema?
- **¿Cuántas cajas** por sucursal? ¿Cajas móviles?
- **¿Hay depósito separado** del salón? ¿Más de un depósito?
- **¿Trabajan con reposición continua** o por turnos (mañana/tarde/noche)?
- **¿Qué pasa con mercadería dañada/rota** (merma)? ¿Quién la autoriza?

### Roles y permisos

- **¿Qué puede hacer un Repositor** exactamente?
  - ¿Solo “Reposición”? ¿Puede ajustar stock?
  - ¿Puede ver costos?
  - ¿Puede anular reposiciones?
- **¿Qué puede hacer un Cajero**?
  - ¿Puede anular ventas?
  - ¿Puede hacer devoluciones?
- **¿Quién confirma un remito**? (depósito, compras, supervisor)
- **¿Hay auditoría obligatoria**? (log de quién hizo qué)

### Trazabilidad (lo que más les importa)

- **¿Qué entienden por “trazabilidad”** en su operación?
  - (A) Solo un historial de “eventos” por producto
  - (B) Trazabilidad “contable” con ubicaciones y saldos por ubicación
  - (C) Trazabilidad “lote/serie” estricta (farmacia/fiambres, etc.)
- **¿La trazabilidad será por**:
  - Código de barras (EAN)
  - Lote
  - Número de serie
  - Fecha de vencimiento
  - Combinación (¿cuál es obligatoria para qué rubros?)
- **¿Quieren poder responder estas preguntas?**
  - “¿Cuándo entró este artículo y por qué remito/OC?”
  - “¿Quién lo repuso y en qué góndola/pasillo?”
  - “¿En qué venta salió y por qué caja/usuario?”
  - “¿Cuántas veces fue repuesto en el día?”
  - “¿Qué mercadería venció/está por vencer y dónde está?”
- **¿Qué nivel de detalle de ubicación necesitan?**
  - Depósito vs Sala (simple)
  - Pasillo/Góndola/Estante (medio)
  - Ubicación exacta por planograma (avanzado)
- **¿Cómo se registra reposición hoy?**
  - ¿Se escanea EAN?
  - ¿Se carga cantidad por bulto/caja/unidad?
  - ¿Se registra ubicación?
  - ¿Se firma con usuario?
- **¿Quieren trazabilidad de devoluciones** (cliente devuelve producto)?
- **¿Quieren trazabilidad de transferencias internas** (depósito → sala) que afecte stock por ubicación?

### Stock: reglas de negocio (críticas)

- **¿StockActual representa qué?**
  - ¿Total empresa? ¿Total sucursal? ¿Solo sala? ¿incluye depósito?
- **¿StockDeposito se usa hoy** o es solo un campo futuro?
- **¿Permiten stock negativo** en caja? Si sí, ¿hasta qué límite y quién lo autoriza?
- **¿Cómo manejan “bultos”** (unidades por bulto, cajas por bulto)?
- **¿Quieren sugerencias de compra** en base a mínimo/máximo?

### Compras / OC / Remitos

- **¿Siempre hay Orden de Compra** antes del remito?
- **¿La recepción puede ser parcial** por múltiples entregas?
- **¿Necesitan capturar datos del remito del proveedor** obligatoriamente (número, transportista, etc.)?
- **¿Se carga precio costo en recepción** o lo trae compra/lista?

### Ventas

- **¿Devoluciones / notas de crédito**: ¿cómo las hacen hoy?
- **¿Descuentos**: por artículo, por total, por cliente, por medio de pago, por promo?
- **¿Control de edad/restricciones** (alcohol, etc.)?
- **¿Fiscal**: ¿qué tipos de comprobante usan y cuáles requieren CAE?

### Hardware / operación en caja

- **¿Qué lector de código de barras** usan?
- **¿Impresora térmica**: modelo, driver, formato de ticket.
- **¿Balanza** (pesables): ¿marca/protocolo?
- **¿Cajón de dinero**?
- **¿Necesitan modo offline** si cae internet/API?

### Reportes y auditoría

- **¿Qué reportes son “sí o sí”** para la primera versión?
- **¿Quieren exportar a Excel** o PDF?
- **¿Necesitan logs de auditoría** (quién tocó stock, quién anuló, etc.)?

---

## Próximos pasos recomendados (para cerrar alcance)

- Definir **modelo de ubicaciones** (simple vs catálogo) para reposición.
- Decidir si la trazabilidad será **solo eventos** o también **stock por ubicación**.
- Agregar pantalla “Trazabilidad por producto” (buscar por EAN y ver timeline).
- Agregar pantalla “Reposición” para perfil Repositor:
  - escaneo EAN → cantidad → ubicación → registrar evento.

---

## Notas técnicas

- Migraciones EF viven en `SuperPOS/src/SuperPOS.API/Migrations`.
- En esta iteración se agregó: `AddTrazabilidadEventos`.

---

## Despliegue en la PC del cliente

### Arquitectura de instalación (un solo local)

- **Una PC "servidor"**: corre PostgreSQL + `SuperPOS.API` (como Windows Service, ver abajo). IP fija en la red del local.
- **Cada caja**: solo `SuperPOS.Client`, con `appsettings.json` → `ApiBaseUrl` apuntando a `http://IP-DEL-SERVIDOR:5075`.
- Abrir el puerto 5075 en el Firewall de Windows de la PC servidor (regla de entrada, TCP).

### Publicar los binarios

```bash
dotnet publish SuperPOS/src/SuperPOS.API/SuperPOS.API.csproj -c Release -r win-x64 --self-contained -o publish/api
dotnet publish SuperPOS/src/SuperPOS.Client/SuperPOS.Client.csproj -c Release -r win-x64 --self-contained -o publish/client
```

### Instalar la API como Windows Service (PC servidor)

El proyecto ya soporta correr como servicio (`UseWindowsService()` en `Program.cs`) — sobrevive a reinicios y a que alguien cierre sesión, a diferencia de correrlo como consola. Instalación (PowerShell como Administrador, una sola vez):

```powershell
sc.exe create "SuperPOS API" binPath= "C:\ruta\publish\api\SuperPOS.API.exe" start= auto
sc.exe description "SuperPOS API" "API central de SuperPOS (ventas, stock, AFIP)"
sc.exe start "SuperPOS API"
```

Para desinstalar: `sc.exe stop "SuperPOS API"` seguido de `sc.exe delete "SuperPOS API"`.

### Variables de entorno requeridas (no van en appsettings.json commiteado)

Configurar en la PC servidor antes de instalar el servicio (`setx VARIABLE valor /M`, requiere reabrir sesión):

- `ConnectionStrings__DefaultConnection` — cadena de conexión a Postgres con el usuario/password reales.
- `Jwt__Secret` — secreto de firma JWT (generar uno propio por instalación, no reusar el de desarrollo).
- `Afip__PasswordCertificado` — password del certificado `.p12` de AFIP.
- `DeepSeek__ApiKey` — si se usa el asistente de IA.

