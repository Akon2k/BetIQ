import requests
import json
from datetime import datetime
import time
import sys

# API-Football Settings
API_KEY = "613c5a9ef5msh9e050a2ae902d5dp1df25fjsn4752bc66080f"
API_URL_BASE = "https://v3.football.api-sports.io" 

# BetIQ API Settings
BETIQ_API_URL_REGISTRAR = "http://localhost:5023/api/Futbol/registrar"
BETIQ_API_URL_RESULTADO = "http://localhost:5023/api/Futbol/{}/resultado"

def get_headers():
    return {
        "x-rapidapi-host": "api-football-v1.p.rapidapi.com",
        "x-rapidapi-key": API_KEY
    }

def obtener_partidos_por_liga_y_temporada(league_id, season):
    url = f"https://api-football-v1.p.rapidapi.com/v3/fixtures?league={league_id}&season={season}"
    try:
        response = requests.get(url, headers=get_headers(), timeout=15)
        if response.status_code == 200:
            return response.json()
        print(f"\nError obteniendo liga {league_id} temp {season}: HTTP {response.status_code}")
    except Exception as e:
        print(f"\nExcepción en petición: {e}")
    return None

def procesar_respuesta_api_football(datos):
    partidos_procesados = []
    if not datos or 'response' not in datos:
        return partidos_procesados

    for item in datos['response']:
        fixture = item.get('fixture', {})
        teams = item.get('teams', {})
        goals = item.get('goals', {})
        
        estado = fixture.get('status', {}).get('short', '')
        # Solo queremos procesar partidos finalizados (FT, AET, PEN) para historial ELO
        if estado not in ['FT', 'AET', 'PEN']: 
            continue
            
        equipo_local = teams.get('home', {}).get('name')
        equipo_visitante = teams.get('away', {}).get('name')
        
        # En API-Football, a veces viene nulo si no han jugado. Pero ya filtramos por finalizados.
        goles_local = goals.get('home', 0)
        goles_visitante = goals.get('away', 0)
        if goles_local is None: goles_local = 0
        if goles_visitante is None: goles_visitante = 0
        
        fecha_evento = fixture.get('date')
        
        partidos_procesados.append({
            'equipoLocal': equipo_local,
            'equipoVisitante': equipo_visitante,
            'fechaEvento': fecha_evento,
            'golesLocal': goles_local,
            'golesVisitante': goles_visitante,
            'estado': 'Finalizado'
        })
    return partidos_procesados

def inyectar_en_betiq(partidos):
    headers = {'Content-Type': 'application/json'}
    exitosos = 0
    total = len(partidos)
    
    for i, p in enumerate(partidos):
        # 1. Registrar el partido (las fuerzas las ponemos en 1.0 ya que solo calibramos ELO aquí)
        payload_registro = {
            "equipoLocal": p['equipoLocal'],
            "equipoVisitante": p['equipoVisitante'],
            "fechaEvento": p['fechaEvento'],
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
                    # 2. Registrar Resultado para activar el motor ELO
                    payload_resultado = {
                        "puntosLocal": p['golesLocal'], # El backend reusa NbaMatchResultDto (puntos=goles)
                        "puntosVisitante": p['golesVisitante'], 
                        "estado": "Finalizado"
                    }
                    url_resultado = BETIQ_API_URL_RESULTADO.format(match_id)
                    requests.put(url_resultado, data=json.dumps(payload_resultado), headers=headers)
                    if i % 50 == 0:
                        sys.stdout.write(f"\n[Progreso] {i}/{total} procesados... ")
                    sys.stdout.write(".")
                    sys.stdout.flush()
                    exitosos += 1
                else:
                    sys.stdout.write("?") # Sin ID devuelto
                    sys.stdout.flush()
            else:
                sys.stdout.write("E") # Error al registrar
                sys.stdout.flush()
        except:
             sys.stdout.write("X") # Excepción de red hacia BetIQ
             sys.stdout.flush()
             
    return exitosos

if __name__ == "__main__":
    print("=========================================================")
    print(" INGESTA HISTÓRICA DE FÚTBOL (Top Ligas) - BetIQ API ")
    print("=========================================================")
    print("Optimizador de API activado: Consultando por Temporada.")
    print("Fuente: API-Football (RapidAPI)\n")
    
    # Ligas top a procesar (ID de API-football)
    LIGAS_TOP = [
        {"id": 39, "nombre": "Premier League (Inglaterra)"},
        {"id": 140, "nombre": "La Liga (España)"},
        {"id": 135, "nombre": "Serie A (Italia)"},
        {"id": 78, "nombre": "Bundesliga (Alemania)"},
        {"id": 2, "nombre": "UEFA Champions League"}
    ]
    
    TEMPORADAS = [2022, 2023, 2024] # Últimos ~3 años
    
    todos_los_partidos = []
    
    # RONDAS DE EXTRACCIÓN
    print("Fase 1: Extrayendo calendarios de API-Football...")
    for liga in LIGAS_TOP:
        for temporada in TEMPORADAS:
            print(f" Consultando -> {liga['nombre']} | {temporada}")
            datos = obtener_partidos_por_liga_y_temporada(liga['id'], temporada)
            partidos_liga_temp = procesar_respuesta_api_football(datos)
            todos_los_partidos.extend(partidos_liga_temp)
            print(f"   => Obtenidos {len(partidos_liga_temp)} partidos finalizados.")
            time.sleep(1) # Respetar rate limits de RapidAPI
            
    print(f"\nFase 2: Ordenando {len(todos_los_partidos)} partidos cronológicamente...")
    # Ordenamiento cronológico global para un ELO cross-league perfecto (ej. Champions League vs Ligas locales)
    todos_los_partidos.sort(key=lambda x: x['fechaEvento'])
    
    print(f"Fase 3: Inyectando a BetIQ.API de forma cronológica los {len(todos_los_partidos)} eventos...")
    # Lote de inyección
    total_registrados = inyectar_en_betiq(todos_los_partidos)
    
    print(f"\n\n=========================================================")
    print(f" COMPLETADO: Se inyectaron exitosamente {total_registrados} partidos de Fútbol.")
    print(" Tu BetIQ ELO Ranking ahora está altamente calibrado internacionalmente.")
    print("=========================================================")
