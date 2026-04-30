import requests
import json
from datetime import datetime, timedelta
import time
import sys

# URLs de la API de BetIQ
BETIQ_API_URL_REGISTRAR = "http://localhost:5023/api/nbamatches/registrar"
BETIQ_API_URL_RESULTADO = "http://localhost:5023/api/nbamatches/{}/resultado"

# API "oculta" de ESPN que no requiere API Key
ESPN_API_URL = "http://site.api.espn.com/apis/site/v2/sports/basketball/nba/scoreboard"

def obtener_partidos_espn(fecha_str):
    url = f"{ESPN_API_URL}?dates={fecha_str}"
    try:
        response = requests.get(url, timeout=10)
        if response.status_code == 200:
            return response.json()
    except requests.exceptions.RequestException:
        pass
    return None

def limpiar_nombre_equipo(nombre):
    if "Trail Blazers" in nombre: return "Blazers"
    return nombre.split(" ")[-1] if " " in nombre else nombre

def procesar_partidos(datos_espn):
    partidos_procesados = []
    if not datos_espn or 'events' not in datos_espn or len(datos_espn['events']) == 0:
        return partidos_procesados

    for evento in datos_espn['events']:
        # Solo procesar partidos finalizados o en pre para extraer
        estado = evento['status']['type']['state']
        if estado not in ['post', 'pre', 'in']:
            continue
            
        competicion = evento['competitions'][0]
        competidores = competicion['competitors']
        
        try:
            equipo_local_datos = next(c for c in competidores if c['homeAway'] == 'home')
            equipo_visitante_datos = next(c for c in competidores if c['homeAway'] == 'away')
            
            equipo_local = limpiar_nombre_equipo(equipo_local_datos['team']['name'])
            equipo_visitante = limpiar_nombre_equipo(equipo_visitante_datos['team']['name'])
            
            # Obtener estadísticas avanzadas si existen
            score_local = int(equipo_local_datos.get('score', 0))
            score_visitante = int(equipo_visitante_datos.get('score', 0))
            
            fecha_evento = evento['date']
            
            partidos_procesados.append({
                'equipoLocal': equipo_local,
                'equipoVisitante': equipo_visitante,
                'fechaEvento': fecha_evento,
                'estado': estado,
                'scoreLocal': score_local,
                'scoreVisitante': score_visitante
            })
        except Exception as e:
            continue
            
    return partidos_procesados

def inyectar_en_betiq(partidos):
    headers = {'Content-Type': 'application/json'}
    exitosos = 0
    
    for p in partidos:
        payload_registro = {
            "equipoLocal": p['equipoLocal'],
            "equipoVisitante": p['equipoVisitante'],
            "fechaEvento": p['fechaEvento'],
            "eficienciaOfensivaLocal": 0, "eficienciaDefensivaLocal": 0, "promedioPuntosTotal": 0
        }
        
        try:
            res_registro = requests.post(BETIQ_API_URL_REGISTRAR, data=json.dumps(payload_registro), headers=headers)
            
            if res_registro.status_code in [200, 201]:
                betiq_match = res_registro.json()
                # Extraemos el id del partido, que antes se enviaba como id o idPartido
                # Dependemos de lo que retorne la API
                match_id = betiq_match.get('idPartido') or betiq_match.get('id')
                
                if p['estado'] == 'post' and match_id:
                    payload_resultado = {
                        "puntosLocal": p['scoreLocal'], 
                        "puntosVisitante": p['scoreVisitante'], 
                        "estado": "Finalizado"
                    }
                    url_resultado = BETIQ_API_URL_RESULTADO.format(match_id)
                    requests.put(url_resultado, data=json.dumps(payload_resultado), headers=headers)
                    sys.stdout.write(".")
                    sys.stdout.flush()
                else:
                    sys.stdout.write("P") # Programado
                    sys.stdout.flush()
                exitosos += 1
            else:
                sys.stdout.write("E") # Error
                sys.stdout.flush()
        except:
             sys.stdout.write("X") # Excepción
             sys.stdout.flush()
             
    return exitosos

if __name__ == "__main__":
    print("=========================================================")
    print(" INGESTA HISTÓRICA DE NBA (Últimos 2 Años) - BetIQ API ")
    print("=========================================================")
    print("Esta herramienta reconstruirá el ELO de todos los equipos")
    print("procesando resultados pasados en orden cronológico.")
    print("Fuente: ESPN Scoreboard API (Gratuita)\n")
    
    dias_historia = 730 # 2 años
    fecha_inicio = datetime.now() - timedelta(days=dias_historia)
    fecha_fin = datetime.now()
    
    print(f"-> Rango: Desde {fecha_inicio.strftime('%Y-%m-%d')} hasta {fecha_fin.strftime('%Y-%m-%d')}")
    print(f"-> Total de días a analizar: {dias_historia}")
    print("-> Iniciando ciclo de extracción e inyección...\n")
    
    total_registrados = 0
    fecha_actual = fecha_inicio
    
    # Optimizador: En meses de verano (Julio/Agosto) la NBA no juega, podemos acelerar si no hay eventos
    # Pero para no perder Play-In u otros torneos, haremos barrido día a día.
    
    while fecha_actual <= fecha_fin:
        fecha_str = fecha_actual.strftime("%Y%m%d")
        
        datos = obtener_partidos_espn(fecha_str)
        partidos = procesar_partidos(datos)
        
        if partidos:
            sys.stdout.write(f"\n{fecha_actual.strftime('%Y-%m-%d')} | Encontrados {len(partidos)} partidos ")
            sys.stdout.flush()
            
            # Inyectar cronológicamente en nuestra API para que el Elo fluya naturalmente
            # Ordenar los partidos de ese día por hora de evento (aunque 'date' ya los trae ordenados)
            partidos.sort(key=lambda x: x['fechaEvento'])
            
            inyectados = inyectar_en_betiq(partidos)
            total_registrados += inyectados
            time.sleep(0.1) # Breve pausa para no saturar 
        
        fecha_actual += timedelta(days=1)
        
    print(f"\n\n=========================================================")
    print(f" COMPLETADO: Se inyectaron exitosamente {total_registrados} partidos.")
    print(" Tu BetIQ ELO Ranking ahora está altamente calibrado.")
    print("=========================================================")
