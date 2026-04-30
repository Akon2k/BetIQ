# Script de Inicio Limpio para BetIQ API y Frontend
# Mata procesos "colgados" en el puerto 5023 antes de levantar

Write-Host "Iniciando secuencia de limpieza..." -ForegroundColor Cyan

# 1. Buscar PIDs usando los puertos 5023 y 8080
$ports = @(5023, 8080)
foreach ($port in $ports) {
    $pidsToKill = (netstat -ano | Select-String ":$port " | ForEach-Object {
        $parts = $_.Line.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
        $parts[-1]
    }) | Select-Object -Unique

    # 2. Matar el proceso si existe
    if ($pidsToKill) {
        Write-Host "-> Encontrado zombie en el puerto $port. Exterminando..." -ForegroundColor Yellow
        foreach ($pidToKill in $pidsToKill) {
            if ($pidToKill -ne "0") {
                try {
                    Stop-Process -Id $pidToKill -Force -ErrorAction Stop
                    Write-Host "   Zombie PID $pidToKill eliminado con éxito." -ForegroundColor Green
                } catch {
                    Write-Host "   No se pudo cerrar el PID $pidToKill. Puede requerir permisos de Administrador." -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "-> El puerto $port está completamente libre." -ForegroundColor Green
    }
}

# 3. Levantar API en segundo plano
Write-Host "-> Levantando Backend (BetIQ.API)..." -ForegroundColor Cyan
Set-Location -Path "$PSScriptRoot\BetIQ.API"
Start-Process dotnet "run" -WindowStyle Minimized

# Damos un par de segundos para que el servidor complete su inicio
Start-Sleep -Seconds 3

# 4. Levantar la Web (Usando dotnet serve en ventana CMD visible)
Write-Host "-> Levantando Web Frontend en puerto 8080..." -ForegroundColor Cyan
Set-Location -Path "$PSScriptRoot\BetIQ.Web"
# Abrimos una ventana CMD real con dotnet serve para que no se cierre sola
Start-Process "cmd.exe" -ArgumentList "/k dotnet serve -p 8080 -a 127.0.0.1" -WindowStyle Normal

Start-Sleep -Seconds 3
Start-Process "http://localhost:8080"

Write-Host "¡TODO LISTO! API Puerto: 5023 | Web Puerto: 8080" -ForegroundColor Green
Write-Host "Cierra las ventanas de consola (dotnet y dotnet-serve) para apagar." -ForegroundColor Gray
Write-Host "Presiona ENTER para salir..."
Read-Host
