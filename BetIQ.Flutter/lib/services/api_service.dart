import 'dart:convert';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:http/http.dart' as http;
import 'package:betiq_flutter/models/equipo.dart';

class ApiService {
  String get baseUrl {
    if (kIsWeb) {
      return "http://localhost:5023/api";
    } else {
      // Assume non-web is Android emulator for this simple case
      return "http://10.0.2.2:5023/api";
    }
  }

  Future<List<Equipo>> getTeams() async {
    final response = await http.get(Uri.parse('$baseUrl/teams'));

    if (response.statusCode == 200) {
      List<dynamic> body = jsonDecode(response.body);
      List<Equipo> teams = body.map((dynamic item) => Equipo.fromJson(item)).toList();
      return teams;
    } else {
      throw "Failed to load teams";
    }
  }
}
