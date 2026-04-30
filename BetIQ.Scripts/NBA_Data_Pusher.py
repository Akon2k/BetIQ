import requests
import thesportsdb.events as tsdb_events
from datetime import datetime

# --- CONFIGURACIÓN ---
API_BASE_URL = "http://localhost:5023/api/NBAMatches"
# ID de la liga NBA en TheSportsDB
LEAGUE_ID = "4387" 

def enviar_partido_nba(partido):
    """
    Envía los datos de un partido obtenido de TheSportsDB a la API de .NET.
    """
    try:
        fecha_str = partido['dateEvent']
        hora_str = partido.get('strTime', "00:00:00")
        
        # Combinar fecha y hora, y convertir a formato ISO 8601
        fecha_evento_dt = datetime.strptime(f"{fecha_str} {hora_str}", "%Y-%m-%d %H:%M:%S")
        fecha_evento_iso = fecha_evento_dt.isoformat()

        payload = {
            "equipoLocal": partido['strHomeTeam'],
            "equipoVisitante": partido['strAwayTeam'],
            # TheSportsDB no provee estos datos en el endpoint de calendario,
            # usamos valores por defecto.
            "eficienciaOfensivaLocal": 105.0,
            "eficienciaDefensivaLocal": 105.0,
            "promedioPuntosTotal": 220.0,
            "fechaEvento": fecha_evento_iso
        }

        print(f"Enviando a BetIQ: {partido['strEvent']}...")
        response = requests.post(f"{API_BASE_URL}/registrar", json=payload, timeout=15)
        
        if response.status_code == 200:
            res_data = response.json()
            print(f"  -> Éxito: {res_data['mensaje']}")
        elif response.status_code == 400 and "ya existe" in response.text.lower():
             print(f"  -> Info: El partido ya existe en la base de datos.")
        else:
            print(f"  -> Error en API al registrar: {response.status_code} - {response.text}")
            
    except requests.exceptions.RequestException as e:
        print(f"  -> Error de conexión: No se pudo conectar a la API. ¿Está corriendo? - {e}")
    except Exception as e:
        print(f"  -> Error inesperado al procesar partido: {e}")

def enviar_resultado_partido(partido):
    """
    Envía el resultado de un partido finalizado a la API de .NET.
    """
    try:
        id_partido_api = partido['idEvent'] # Usamos el ID de TheSportsDB como referencia
        
        payload = {
            "puntosLocal": int(partido['intHomeScore']),
            "puntosVisitante": int(partido['intAwayScore'])
        }

        print(f"Enviando resultado a BetIQ: {partido['strEvent']} ({payload['puntosLocal']}-{payload['puntosVisitante']})")
        # El endpoint de la API espera el ID del partido en la URL
        response = requests.put(f"{API_BASE_URL}/{id_partido_api}/resultado", json=payload, timeout=15)

        if response.status_code == 200:
            res_data = response.json()
            print(f"  -> Éxito: {res_data['mensaje']}")
        elif response.status_code == 404:
            print(f"  -> Info: El partido con ID {id_partido_api} no fue encontrado en la API de BetIQ. Puede que no estuviera registrado.")
        else:
            print(f"  -> Error en API al enviar resultado: {response.status_code} - {response.text}")

    except requests.exceptions.RequestException as e:
        print(f"  -> Error de conexión: No se pudo conectar a la API. ¿Está corriendo? - {e}")
    except Exception as e:
        print(f"  -> Error inesperado al procesar resultado: {e}")


def sync_nba_data():
    """
    Obtiene datos de la NBA desde TheSportsDB y los sincroniza con la API de BetIQ.
    """
    print("--- Sincronizando Próximos Partidos de la NBA ---")
    try:
        # 1. OBTENER PRÓXIMOS PARTIDOS
        proximos_partidos_response = tsdb_events.nextLeagueEvents(LEAGUE_ID)
        if proximos_partidos_response and 'events' in proximos_partidos_response:
            proximos_partidos = proximos_partidos_response['events']
            print(f"Se encontraron {len(proximos_partidos)} próximos partidos.")
            for partido in proximos_partidos:
                print(f"DEBUG: Revisando evento {partido.get('strEvent')} con liga {partido.get('strLeague')}")
                if partido.get('strLeague') == 'NBA':
                    enviar_partido_nba(partido)
        else:
            print("No se encontraron próximos partidos o la respuesta no tiene el formato esperado.")

    except Exception as e:
        print(f"Error al obtener próximos partidos de TheSportsDB: {e}")

    print("\n--- Sincronizando Resultados de Partidos Recientes ---")
    try:
        # 2. OBTENER ÚLTIMOS 5 PARTIDOS FINALIZADOS
        ultimos_resultados_response = tsdb_events.lastLeagueEvents(LEAGUE_ID)
        if ultimos_resultados_response and 'events' in ultimos_resultados_response:
            ultimos_resultados = ultimos_resultados_response['events']
            print(f"Se encontraron {len(ultimos_resultados)} resultados recientes. Procesando los últimos 5...")
            for resultado in ultimos_resultados[:5]:
                # Asegurarnos de que el partido está finalizado y tiene marcador
                if resultado.get('strStatus') == 'Match Finished' and resultado.get('intHomeScore') is not None and resultado.get('strLeague') == 'NBA':
                    enviar_resultado_partido(resultado)
        else:
            print("No se encontraron resultados de partidos recientes o la respuesta no tiene el formato esperado.")

    except Exception as e:
        print(f"Error al obtener resultados de TheSportsDB: {e}")


if __name__ == "__main__":
    print("--- Motor de Datos NBA con TheSportsDB - BetIQ ---")
    sync_nba_data()
