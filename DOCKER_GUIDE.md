# Guía de Docker para BetIQ 🐳

Este documento detalla los servicios que componen el entorno de BetIQ y los comandos necesarios para administrarlos.

## 🚀 Servicios en el Entorno

| Servicio | Contenedor | Tecnología | Puerto Local | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| **Base de Datos** | `betiq-db` | SQL Server 2022 | `1433` | Almacenamiento central de datos. |
| **API Backend** | `betiq-api` | .NET 10 | `5023` | Lógica de negocio y cálculos matemáticos. |
| **Dashboard Web** | `betiq-web` | Nginx | `8080` | Interfaz de usuario (Glassmorphism UI). |

---

## 🛠️ Comandos Principales

### 1. Levantar todo el entorno
Este comando construye las imágenes (si hay cambios) y levanta los contenedores en segundo plano:
```powershell
docker-compose up -d --build
```

### 2. Detener el entorno
Detiene y elimina los contenedores, pero mantiene las imágenes:
```powershell
docker-compose down
```

### 3. Ver estado de los servicios
Verifica si los contenedores están corriendo correctamente:
```powershell
docker-compose ps
```

### 4. Ver logs (en tiempo real)
Útil para depurar si algo no carga:
```powershell
docker-compose logs -f
```

---

## 🔗 Enlaces de Acceso (Una vez levantado)
- **Frontend**: [http://localhost:8080](http://localhost:8080)
- **API Swagger**: [http://localhost:5023/swagger](http://localhost:5023/swagger)
