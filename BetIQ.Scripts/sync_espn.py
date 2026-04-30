import requests
import json
from datetime import datetime, timedelta

# Asegúrate de que tu API esté encendida
BETIQ_API_URL_REGISTRAR = "http://localhost:5023/api/nbamatches/registrar"
BETIQ_API_URL_RESULTADO = "http://localhost:5023/api/nbamatches/{}/resultado"

# Usaremos la API "oculta" de ESPN que no requiere API Key y es totalmente gratis
ESPN_API_URL = "http://site.api.espn.com/apis/site/v2/sports/basketball/nba/scoreboard"

def obtener_partidos_espn(fecha_str=None):
    """Obtiene los partidos de la NBA de una fecha específica."""
    url = ESPN_API_URL
    if fecha_str:
        # Formato de fecha para ESPN: YYYYMMDD
        url += f"?dates={fecha_str}"
        
    try:
        response = requests.get(url)
        response.raise_for_status()
        return response.json()
    except requests.exceptions.RequestException as e:
        print(f"Error al conectar con ESPN: {e}")
        return None

def limpiar_nombre_equipo(nombre):
    """Extrae el nombre corto del equipo (ej: 'Lakers' en lugar de 'Los Angeles Lakers')"""
    if "Trail Blazers" in nombre: return "Blazers"
    return nombre.split(" ")[-1] if " " in nombre else nombre

def sincronizar_partidos(dias_atras=0):
    """
    Descarga los partidos de ESPN y los envía a BetIQ.
    """
    fecha_consulta = datetime.now() - timedelta(days=dias_atras)
    fecha_espn_formato = fecha_consulta.strftime("%Y%m%d")
    
    print(f"\n--- Buscando partidos para la fecha: {fecha_consulta.strftime('%Y-%m-%d')} ---")
    datos_espn = obtener_partidos_espn(fecha_espn_formato)
    
    if not datos_espn or 'events' not in datos_espn or len(datos_espn['events']) == 0:
        print("No se encontraron partidos para esta fecha.")
        return

    for evento in datos_espn['events']:
        competicion = evento['competitions'][0]
        competidores = competicion['competitors']
        
        equipo_local_datos = next(c for c in competidores if c['homeAway'] == 'home')
        equipo_visitante_datos = next(c for c in competidores if c['homeAway'] == 'away')
        
        equipo_local = limpiar_nombre_equipo(equipo_local_datos['team']['name'])
        equipo_visitante = limpiar_nombre_equipo(equipo_visitante_datos['team']['name'])
        
        estado = evento['status']['type']['state'] # 'pre', 'in', 'post'
        
        payload_registro = {
            "equipoLocal": equipo_local,
            "equipoVisitante": equipo_visitante,
            "fechaEvento": evento['date'],
            "eficienciaOfensivaLocal": 0, "eficienciaDefensivaLocal": 0, "promedioPuntosTotal": 0
        }
        
        headers = {'Content-Type': 'application/json'}
        try:
            print(f"[{estado.upper()}] Buscando registrar: {equipo_local} vs {equipo_visitante}...")
            res_registro = requests.post(BETIQ_API_URL_REGISTRAR, data=json.dumps(payload_registro), headers=headers)
            
            if res_registro.status_code in [200, 201]:
                betiq_match = res_registro.json()
                betiq_match_id = betiq_match.get('idPartido') if isinstance(betiq_match, dict) else None
                
                print(f"OK - Registrado exitosamente: {equipo_local} vs {equipo_visitante}")
                
                if estado == 'post' and betiq_match_id:
                    score_local = int(equipo_local_datos['score'])
                    score_visitante = int(equipo_visitante_datos['score'])
                    
                    payload_resultado = {
                        "puntosLocal": score_local, "puntosVisitante": score_visitante, "estado": "Finalizado"
                    }
                    
                    url_resultado = BETIQ_API_URL_RESULTADO.format(betiq_match_id)
                    res_resultado = requests.put(url_resultado, data=json.dumps(payload_resultado), headers=headers)
                    
                    if res_resultado.status_code in [200, 204]:
                        print(f"   Resultado guardado: {equipo_local} ({score_local}) - ({score_visitante}) {equipo_visitante}. ELO actualizado.")
                    else:
                        print(f"   Error guardando resultado: {res_resultado.status_code}")
                elif estado == 'pre':
                    print(f"   Partido programado para más tarde.")
            else:
                print(f"Error al registrar partido: {res_registro.status_code} - {res_registro.text}")
                
        except requests.exceptions.RequestException as e:
            print(f"Error de conexión al registrar: {e}")

if __name__ == "__main__":
    print("Iniciando inyección de datos de ESPN a BetIQ...")
    print("Asegúrate de que la API de BetIQ esté ejecutándose en http://localhost:5023")
    
    print("\n--- Sincronizando historial reciente (Para inicializar ELO) ---")
    # Sincronizar desde 5 días atrás para que el ELO varíe más
    for i in range(5, 0, -1):
        sincronizar_partidos(dias_atras=i)
    
    print("\n--- Sincronizando partidos de HOY ---")
    sincronizar_partidos(dias_atras=0)
    
    print("\nProceso completado. Revisa tu Dashboard para ver los nuevos equipos y puntajes ELO.")
