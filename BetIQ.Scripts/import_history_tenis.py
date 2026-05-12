import requests
import pandas as pd
import io
import time
import sys
from datetime import datetime

BETIQ_API_URL_REGISTRAR = "http://localhost:5023/api/Tenis/registrar"
BETIQ_API_URL_RESULTADO = "http://localhost:5023/api/Tenis/{}/resultado"

def process_tennis_history(start_year=2020, end_year=2026):
    print("=========================================================")
    print(" INGESTA HISTÓRICA DE TENIS (OPEN SOURCE) - BetIQ API ")
    print("=========================================================")
    print(f"Descargando datos ATP desde {start_year} hasta {end_year}...")
    
    # Categorías permitidas: G (Grand Slam), M (Masters 1000), F (Finals), A (ATP 250/500)
    allowed_levels = ['G', 'M', 'F', 'A']
    
    # Mapeo de superficies a español para la API
    surface_map = {
        'Clay': 'Arcilla',
        'Grass': 'Pasto',
        'Hard': 'Dura',
        'Carpet': 'Dura' # Alfombra se trata como dura o indoor
    }
    
    exitosos = 0
    errores = 0

    for year in range(start_year, end_year + 1):
        url = f"https://raw.githubusercontent.com/JeffSackmann/tennis_atp/master/atp_matches_{year}.csv"
        print(f"\nDescargando año {year}...")
        
        try:
            response = requests.get(url)
            if response.status_code != 200:
                print(f"No se pudo descargar el archivo del año {year} (HTTP {response.status_code}).")
                continue
            
            # Leer CSV en pandas
            df = pd.read_csv(io.StringIO(response.text))
            
            # Filtrar por nivel de torneo
            df = df[df['tourney_level'].isin(allowed_levels)]
            
            # Ordenar cronológicamente (tourney_date) y match_num
            df = df.sort_values(by=['tourney_date', 'match_num'])
            
            for index, row in df.iterrows():
                # Variables del CSV
                t_date_str = str(row['tourney_date'])
                # Convertir YYYYMMDD a YYYY-MM-DDT00:00:00Z
                try:
                    fecha = f"{t_date_str[:4]}-{t_date_str[4:6]}-{t_date_str[6:]}T00:00:00Z"
                except:
                    fecha = f"{year}-01-01T00:00:00Z"
                
                winner = str(row['winner_name']).strip()
                loser = str(row['loser_name']).strip()
                torneo = str(row['tourney_name']).strip()
                
                raw_surface = str(row['surface']).strip()
                superficie = surface_map.get(raw_surface, 'Dura')
                
                # 1. Registrar el partido. Asignamos jugador1 = winner, jugador2 = loser
                payload_reg = {
                    "jugador1": winner,
                    "jugador2": loser,
                    "fechaEvento": fecha,
                    "torneo": torneo,
                    "superficie": superficie
                }
                
                try:
                    res_reg = requests.post(BETIQ_API_URL_REGISTRAR, json=payload_reg)
                    if res_reg.status_code == 200:
                        match_id = res_reg.json().get('id')
                        
                        # 2. Registrar el resultado
                        if match_id:
                            # Como jugador1 es el winner, le damos más puntos/sets
                            payload_res = {"puntosLocal": 2, "puntosVisitante": 0}
                            res_res = requests.put(BETIQ_API_URL_RESULTADO.format(match_id), json=payload_res)
                            if res_res.status_code == 200:
                                sys.stdout.write(".")
                                sys.stdout.flush()
                                exitosos += 1
                            else:
                                errores += 1
                    else:
                        errores += 1
                except Exception as e:
                    sys.stdout.write("X")
                    sys.stdout.flush()
                    errores += 1
                    
        except Exception as e:
            print(f"Error procesando el año {year}: {e}")
            
    print(f"\nFinalizado. {exitosos} partidos de tenis procesados exitosamente. Errores: {errores}")

if __name__ == "__main__":
    # Necesitamos instalar pandas temporalmente si no existe
    try:
        import pandas
    except ImportError:
        import subprocess
        print("Instalando pandas...")
        subprocess.check_call([sys.executable, "-m", "pip", "install", "pandas"])
        import pandas as pd
        
    current_year = datetime.now().year
    process_tennis_history(2020, current_year)
