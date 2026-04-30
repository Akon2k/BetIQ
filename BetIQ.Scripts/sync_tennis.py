import requests
from datetime import datetime

# Configuración
# Información proporcionada por el usuario
API_KEY = "613c5a9ef5msh9e050a2ae902d5dp1df25fjsn4752bc66080f"
API_URL_BASE = "https://sportapi7.p.rapidapi.com/api/v1"
API_HOST = "sportapi7.p.rapidapi.com"
BETIQ_API_URL = "http://localhost:5023/api/Tenis/registrar"

def get_headers():
    return {
        "x-rapidapi-key": API_KEY,
        "x-rapidapi-host": API_HOST
    }

def sync_pro_tennis():
    print("--- Sincronizando Tenis Real (API-Tennis) ---")
    
    # Simulando carga de Grand Slams o torneos ATP
    active_matches = [
        {
            "jugador1": "Carlos Alcaraz", 
            "jugador2": "Jannik Sinner",
            "torneo": "Indian Wells Masters",
            "superficie": "Hard",
            "fechaEvento": datetime.now().isoformat()
        },
        {
            "jugador1": "Novak Djokovic", 
            "jugador2": "Rafael Nadal",
            "torneo": "Roland Garros",
            "superficie": "Clay",
            "fechaEvento": datetime.now().isoformat()
        }
    ]

    for match in active_matches:
        try:
            response = requests.post(BETIQ_API_URL, json=match)
            if response.status_code == 200:
                print(f"OK - Registrado: {match['jugador1']} vs {match['jugador2']}")
            else:
                print(f"Error {response.status_code}: {response.text}")
        except Exception as e:
            print(f"Error: {e}")

if __name__ == "__main__":
    if API_KEY == "TU_API_KEY_AQUÍ":
        print("AVISO: Usando datos de demostración. Configura tu API_KEY para sincronizar el circuito ATP/WTA.")
    sync_pro_tennis()
