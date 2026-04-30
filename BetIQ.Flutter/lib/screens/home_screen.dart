import 'package:flutter/material.dart';
import 'package:betiq_flutter/models/equipo.dart';
import 'package:betiq_flutter/services/api_service.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late Future<List<Equipo>> futureTeams;
  final ApiService apiService = ApiService();

  @override
  void initState() {
    super.initState();
    futureTeams = apiService.getTeams();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Equipos de la NBA'),
      ),
      body: Center(
        child: FutureBuilder<List<Equipo>>(
          future: futureTeams,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const CircularProgressIndicator();
            } else if (snapshot.hasError) {
              return Text('Error: ${snapshot.error}');
            } else if (snapshot.hasData) {
              return ListView.builder(
                itemCount: snapshot.data!.length,
                itemBuilder: (context, index) {
                  Equipo equipo = snapshot.data![index];
                  return ListTile(
                    title: Text(equipo.nombreEquipo),
                    trailing: Text('Elo: ${equipo.eloActual}'),
                  );
                },
              );
            } else {
              return const Text('No hay equipos para mostrar.');
            }
          },
        ),
      ),
    );
  }
}
