class Equipo {
  final String nombreEquipo;
  final int eloActual;
  final String deporte;

  Equipo({
    required this.nombreEquipo,
    required this.eloActual,
    required this.deporte,
  });

  factory Equipo.fromJson(Map<String, dynamic> json) {
    return Equipo(
      nombreEquipo: json['nombreEquipo'],
      eloActual: json['eloActual'],
      deporte: json['deporte'],
    );
  }
}
