import requests
import sqlite3
import json
import os
from datetime import datetime

# Configuración (En producción mover a .env)
# Configuración
# Configuración
# IMPORTANTE: Regístrate en RapidAPI y suscríbete a "API-Football"
API_KEY = "613c5a9ef5msh9e050a2ae902d5dp1df25fjsn4752bc66080f"
API_URL_BASE = "https://api-football-v1.p.rapidapi.com/v3"
BETIQ_API_URL = "http://localhost:5023/api/Futbol/registrar"

def get_headers():
    return {
        "X-RapidAPI-Key": API_KEY,
        "X-RapidAPI-Host": "api-football-v1.p.rapidapi.com"
    }

def fetch_top_league_stats(league_id=39): # 39 = Premier League
    print(f"Obteniendo estadísticas para Liga ID: {league_id}...")
    # En producción: response = requests.get(f"{API_URL_BASE}/teams/statistics", headers=get_headers(), params={...})
    pass

def sync_live_matches():
    print("--- Sincronizando Fútbol Real (API-Football) ---")
    
    # Datos de ejemplo que imitan la respuesta de una API real
    # En una implementación final, esto vendría de f"{API_URL_BASE}/fixtures"
    real_matches = [
        {
            "equipoLocal": "Real Madrid", 
            "equipoVisitante": "Barcelona",
            "fuerzaAtaqueLocal": 2.4, "fuerzaDefensaLocal": 0.5,
            "fuerzaAtaqueVisita": 2.1, "fuerzaDefensaVisita": 0.9,
            "fechaEvento": datetime.now().isoformat()
        },
        {
            "equipoLocal": "Inter Milan", 
            "equipoVisitante": "AC Milan",
            "fuerzaAtaqueLocal": 1.8, "fuerzaDefensaLocal": 0.7,
            "fuerzaAtaqueVisita": 1.6, "fuerzaDefensaVisita": 1.1,
            "fechaEvento": datetime.now().isoformat()
        }
    ]

    for match in real_matches:
        try:
            response = requests.post(BETIQ_API_URL, json=match)
            if response.status_code == 200:
                print(f"OK - Registrado: {match['equipoLocal']} vs {match['equipoVisitante']}")
            else:
                print(f"Error {response.status_code}: {response.text}")
        except Exception as e:
            print(f"Error: {e}")

if __name__ == "__main__":
    if API_KEY == "TU_API_KEY_AQUÍ":
        print("AVISO: Usando datos de demostración. Configura tu API_KEY en el script para datos reales.")
    sync_live_matches()
