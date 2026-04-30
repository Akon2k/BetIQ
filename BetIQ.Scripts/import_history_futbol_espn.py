import requests
import json
from datetime import datetime, timedelta
import time
import sys

# BetIQ API Settings
BETIQ_API_URL_REGISTRAR = "http://localhost:5023/api/Futbol/registrar"
BETIQ_API_URL_RESULTADO = "http://localhost:5023/api/Futbol/{}/resultado"

# Ligas top en la API de ESPN
LIGAS_ESPN = [
    "eng.1",             # Premier League
    "esp.1",             # LaLiga
    "ita.1",             # Serie A
    "ger.1",             # Bundesliga
    "fra.1",             # Ligue 1
    "uefa.champions"     # Champions League
]

# Mapa ESPN ID -> Nombre visible (usado en BD y selector del frontend)
LIGA_NOMBRES = {
    "eng.1": "Premier League",
    "esp.1": "LaLiga",
    "ita.1": "Serie A",
    "ger.1": "Bundesliga",
    "fra.1": "Ligue 1",
    "uefa.champions": "Champions League"
}

def procesar_eventos_espn(datos, liga_id):
    """Extrae partidos finalizados del JSON de ESPN, incluyendo el nombre de la liga."""
    partidos_procesados = []
    liga_nombre = LIGA_NOMBRES.get(liga_id, liga_id)  # Nombre legible de la liga
    if not datos or 'events' not in datos:
        return partidos_procesados

    for evento in datos['events']:
        estado = evento.get('status', {}).get('type', {}).get('state', '')
        if estado != 'post':
            continue
            
        try:
            competitors = evento['competitions'][0]['competitors']
            home_team = next(c for c in competitors if c['homeAway'] == 'home')
            away_team = next(c for c in competitors if c['homeAway'] == 'away')
            
            equipo_local = home_team['team']['name']
            equipo_visitante = away_team['team']['name']
            goles_local = int(home_team.get('score', 0))
            goles_visitante = int(away_team.get('score', 0))
            fecha_evento = evento.get('date')
            
            partidos_procesados.append({
                'equipoLocal': equipo_local,
                'equipoVisitante': equipo_visitante,
                'fechaEvento': fecha_evento,
                'golesLocal': goles_local,
                'golesVisitante': goles_visitante,
                'estado': 'Finalizado',
                'liga': liga_nombre  # <-- Campo que alimenta el selector del frontend
            })
        except Exception as e:
            continue
            
    return partidos_procesados

def inyectar_en_betiq(partidos):
    headers = {'Content-Type': 'application/json'}
    exitosos = 0
    total = len(partidos)
    
    for i, p in enumerate(partidos):
        payload_registro = {
            "equipoLocal": p['equipoLocal'],
            "equipoVisitante": p['equipoVisitante'],
            "fechaEvento": p['fechaEvento'],
            "liga": p.get('liga', ''),       # <-- Nombre de la liga para el selector
            "fuerzaAtaqueLocal": 1.0, 
            "fuerzaDefensaLocal": 1.0, 
            "fuerzaAtaqueVisita": 1.0, 
            "fuerzaDefensaVisita": 1.0
        }
        try:
            res_registro = requests.post(BETIQ_API_URL_REGISTRAR, data=json.dumps(payload_registro), headers=headers)
            if res_registro.status_code in [200, 201]:
                betiq_match = res_registro.json()
                match_id = betiq_match.get('idPartido') or betiq_match.get('id')
                
                if match_id:
                    payload_resultado = {
                        "puntosLocal": p['golesLocal'],
                        "puntosVisitante": p['golesVisitante'], 
                        "estado": "Finalizado"
                    }
                    url_resultado = BETIQ_API_URL_RESULTADO.format(match_id)
                    requests.put(url_resultado, data=json.dumps(payload_resultado), headers=headers)
                    if i % 100 == 0:
                        sys.stdout.write(f"\n[Progreso] {i}/{total} procesados... ")
                    sys.stdout.write(".")
                    sys.stdout.flush()
                    exitosos += 1
                else:
                    sys.stdout.write("?")
                    sys.stdout.flush()
            else:
                sys.stdout.write("E")
                sys.stdout.flush()
        except:
             sys.stdout.write("X")
             sys.stdout.flush()
    return exitosos

if __name__ == "__main__":
    print("=========================================================")
    print(" INGESTA HISTÓRICA DE FÚTBOL (Top Ligas) - BetIQ API ")
    print("=========================================================")
    print("Fuente: API Oculta ESPN (Sin Limits, 100% Gratis)\n")
    
    # Rango de unos 2 años por intervalos de 3 meses para evitar límite de ESPN
    fecha_fin = datetime.now()
    fecha_inicio = fecha_fin - timedelta(days=730)
    
    intervalos = []
    fecha_temp = fecha_inicio
    while fecha_temp < fecha_fin:
        siguiente = fecha_temp + timedelta(days=90)
        if siguiente > fecha_fin:
            siguiente = fecha_fin
        intervalos.append((fecha_temp, siguiente))
        fecha_temp = siguiente + timedelta(days=1)
    
    todos_los_partidos = []
    
    print("Fase 1: Extrayendo resultados de ESPN por intervalos...")
    for liga in LIGAS_ESPN:
        print(f"\n>> Consultando Liga: {liga}")
        for (inicio, fin) in intervalos:
            rango_str = f"{inicio.strftime('%Y%m%d')}-{fin.strftime('%Y%m%d')}"
            url = f"http://site.api.espn.com/apis/site/v2/sports/soccer/{liga}/scoreboard?dates={rango_str}&limit=600"
            try:
                res = requests.get(url, timeout=15)
                if res.status_code == 200:
                    datos = procesar_eventos_espn(res.json(), liga)  # <-- Pasamos el liga_id
                    todos_los_partidos.extend(datos)
                    sys.stdout.write(f" [{inicio.strftime('%b %Y')}: {len(datos)}]")
                else:
                    sys.stdout.write(f" [Error {res.status_code}]")
                sys.stdout.flush()
            except:
                sys.stdout.write(" [Excepción]")
                sys.stdout.flush()
            time.sleep(0.5)
            
    # Borrar duplicados si los hubiera por solapamiento
    unicos = { (p['equipoLocal'], p['equipoVisitante'], p['fechaEvento']): p for p in todos_los_partidos }
    partidos_finales = list(unicos.values())
            
    print(f"\n\nFase 2: Ordenando {len(partidos_finales)} partidos cronológicamente...")
    partidos_finales.sort(key=lambda x: x['fechaEvento'])
    
    print(f"Fase 3: Inyectando a BetIQ.API (Construyendo ELO Football)...")
    total_registrados = inyectar_en_betiq(partidos_finales)
    
    print(f"\n\n=========================================================")
    print(f" COMPLETADO: Se inyectaron exitosamente {total_registrados} partidos de Fútbol.")
    print(" Tu BetIQ ELO Ranking ahora está altamente calibrado internacionalmente.")
    print("=========================================================")
