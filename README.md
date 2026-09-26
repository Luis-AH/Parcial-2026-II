# Plataforma de Incidencias — BiciOperaciones

Sistema web de gestión de incidencias operativas para una red de bicicletas compartidas. Desarrollado con **ASP.NET Core MVC** e integrado con servicios en la nube para búsqueda, caché y comunicación en tiempo real.

---

## Tecnologías utilizadas

| Capa | Tecnología |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10) |
| Base de datos | SQLite (vía Entity Framework Core) |
| Autenticación | ASP.NET Core Identity |
| Búsqueda | Algolia (SDK oficial) |
| Caché | Redis (StackExchange.Redis + IDistributedCache) |
| WebSockets | PieSocket (protocolo Pusher-compatible) |
| Despliegue | Render.com (capa gratuita) |

---

## Cómo ejecutar localmente

### Prerrequisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Pasos

```bash
git clone https://github.com/Luis-AH/Parcial-2026-II.git
cd Parcial-2026-II
dotnet run
```

La aplicación arrancará en `http://localhost:5230`.

**Credenciales del supervisor (generadas por el DataSeeder):**
- Email: `supervisor@bicioperaciones.com`
- Contraseña: `Admin123!`

> **Nota:** Sin una variable de entorno `ConnectionStrings__Redis`, la aplicación usa automáticamente una caché en memoria como fallback. Las funciones de Algolia y PieSocket requieren las variables de entorno correspondientes.

---

## Variables de entorno (producción en Render)

Configurar en el dashboard de Render → Environment:

```
ConnectionStrings__DefaultConnection = DataSource=app.db;Cache=Shared
ConnectionStrings__Redis             = <tu_redis_connection_string>
Algolia__AppId                       = PLHOKYL1K3
Algolia__WriteApiKey                 = <server_side_only>
Algolia__SearchApiKey                = 8c02deba98a51dde0fa714aedb8a73e5
Algolia__IndexName                   = incidencias
PieSocket__AppId                     = 25071
PieSocket__ApiKey                    = jFcqhaByFMQp5iP05LgFlOwnBBWGJuFTcOflwHiO
PieSocket__ApiSecret                 = <server_side_only>
PieSocket__ClusterId                 = free.blr2
PieSocket__Channel                   = incidencias-channel
```

---

## Estrategia de ramas

Las tres funcionalidades se desarrollaron en ramas paralelas que nacen del **mismo commit inicial de `main`**, generando conflictos controlados al fusionarlas:

```
main (commit inicial)
├── feature/busqueda-algolia   → Rama A: búsqueda de texto completo
├── feature/cache-redis        → Rama B: caché de 60s + invalidación
└── feature/websocket-piesocket → Rama C: actualizaciones en tiempo real
```

---

## Resolución de Conflictos

Al fusionar las tres ramas secuencialmente se produjeron **dos conflictos** en el archivo `Views/Operaciones/Incidencias.cshtml`, concretamente en la línea del encabezado `<h1>` que cada rama modificó intencionalmente.

---

### Resolución 1 — Merge de `feature/cache-redis` en `main` (después de Algolia)

**Archivo:** `Views/Operaciones/Incidencias.cshtml`

El conflicto se produjo porque:
- **HEAD (main con Algolia)** modificó el `<h1>` a `"Incidencias abiertas encontradas"` y añadió un formulario `GET` con el parámetro `?q=` para la búsqueda.
- **feature/cache-redis** modificó el `<h1>` a `"Incidencias abiertas con consulta rápida"` y mantuvo un `<input>` estático sin envío al servidor.

**Marcadores de conflicto generados por Git:**

```
<<<<<<< HEAD
<!-- RESOLUCIÓN 1: ... -->
<h1>Incidencias abiertas encontradas</h1>

<div class="controls">
    <form method="get" action="/Operaciones/Incidencias" style="display:contents;">
        <div class="search-box">
            <input id="search-input" type="text" name="q" value="@ViewBag.Query"
                   placeholder="Buscar por nombre de estación o descripción (Algolia)...">
        </div>
        <button type="submit" class="btn">Buscar</button>
    </form>
=======
<h1>Incidencias abiertas con consulta rápida</h1>

<div class="controls">
    <div class="search-box">
        <input id="search-input" type="text" placeholder="Filtrar...">
    </div>
>>>>>>> feature/cache-redis
```

**Decisión de resolución:**

Se conservó el bloque `HEAD` (Rama A) porque:
1. El formulario `GET` con `name="q"` permite que las búsquedas sean rastreables por URL.
2. El `placeholder` es más descriptivo al mencionar que usa Algolia.
3. El controlador ya fue actualizado para manejar el parámetro `?q=` con Algolia.

En el controlador, la resolución integró **ambas lógicas**: Algolia para búsquedas con texto, y Redis como caché para el listado general.

---

### Resolución 2 — Merge de `feature/websocket-piesocket` en `main` (después de Algolia + Redis)

**Archivo:** `Views/Operaciones/Incidencias.cshtml`

El conflicto se produjo porque:
- **HEAD (main con Algolia + Redis)** tenía el `<h1>` como `"Incidencias abiertas encontradas"`.
- **feature/websocket-piesocket** lo había cambiado a `"Incidencias abiertas en tiempo real"`.

**Marcadores de conflicto generados por Git:**

```
<<<<<<< HEAD
<!-- RESOLUCIÓN 1: ... -->
<h1>Incidencias abiertas encontradas</h1>
=======
<h1>Incidencias abiertas en tiempo real</h1>
>>>>>>> feature/websocket-piesocket
```

**Decisión de resolución:**

Se descartaron **ambas** versiones en conflicto y se eligió un título nuevo y definitivo:

```html
<h1>Panel de Incidencias Abiertas</h1>
```

**Justificación:** Con las tres funcionalidades fusionadas (búsqueda Algolia, caché Redis y WebSocket PieSocket), ninguno de los títulos anteriores describía correctamente el sistema completo. El nuevo título es neutro, profesional y aplica a todas las funcionalidades integradas.

En el controlador, la resolución final integró el flujo completo al cerrar una incidencia:
1. Actualizar estado en SQLite.
2. Invalidar caché en Redis (`RemoveAsync`).
3. Eliminar del índice de Algolia.
4. Publicar evento `incidencia-cerrada` via PieSocket para actualizar todos los clientes en tiempo real.

---

## Historial de ramas

Para verificar el grafo completo de commits y merges:

```bash
git log --graph --oneline --all
```

---

## Decisiones arquitectónicas clave

| Regla | Implementación |
|---|---|
| DB efímera en Render | `DataSeeder` con `EnsureCreated` + GUIDs fijos hardcodeados para el usuario y las incidencias |
| Serialización JSON sin ciclos | `ReferenceHandler.IgnoreCycles` en `AddJsonOptions` |
| Claves secretas nunca al frontend | `WriteApiKey` de Algolia y `ApiSecret` de PieSocket solo en `IConfiguration` |
| Payload camelCase en WebSocket | `new { id }` — la propiedad en minúscula coincide con `data.id` en JavaScript |
| Sin HTTPS redirect en Render | El reverse proxy de Render maneja TLS; la app solo escucha en HTTP en `0.0.0.0:$PORT` |
