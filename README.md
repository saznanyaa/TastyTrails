# TastyTrail
## Pregled projekta
TastyTrail je web aplikacija za istraživanje popularnih restorana, koja omogućava korisnicima da pretražuju restorane, prate njihovu popularnost, ostavljaju recenzije, i čuvaju omiljene restorane.
Sistem koristi različite baze podataka, radi optimizacije različitih delova aplikacije, i omogućavanja bržih i skalabilnijih upita.
## Tehnologije
Frontend: React
Backend: ASP.NET Core Web API
Baze podataka: 
- MongoDB (glavni podaci o korisnicima, restoranima i recenzijama)
- Cassandra (vremenski bazirani podaci vezani za istoriju i nove upite)
- Neo4j (relacije i preporuke)
## Funkcionalnosti
-	registracija i prijava korisnika
- praćenje i pregled drugih korisnika
- prikaz i pregled restorana
- ostavljanje ocena i komentara
- čuvanje restorana
- prikaz popularnih restorana
- prikaz preporučenih restorana, na osnovu različitih kriterijuma
## Pokretanje projekta
1. Backend:
   - otvoriti solution u VS Code-u, ili Visual Studio-u
   - proveriti konekcione stringove u appsettings.Development.json-u (ili appsettings.json-u)
   - pokrenuti baze podataka
   - pokretanje projekta pomoću: dotnet run
2. Frontend:
   - pozicionirati se u tastytrail-frontend folder u cmd-u
   - uneti komande: npm install, zatim npm run dev
## Testiranje
Testiranje API-a je odrađeno pomoću Swagger-a i Postman-a, frontend funkcionalnosti su testirane ručno, sa proverom podataka direktno u bazama podataka.
## Uputstvo za korišćenje
Aplikacija se pokreće na Home stranici, koja zahteva odabir jednog od ponuđenih gradova.
![Home Page](TastyTrails/slike/homepage.png)
Klik na jednu od ovih kartica, vodi korisnika na explore/city stranu, koja prikazuje restorane koji se nalaze u bazama podataka za određeni grad.
Najpre se proverava da li u bazi podataka postoje restorani za dati grad (Cassandra), i ukoliko ne, restorani se pribavljaju pomoću OpenStreetMap Overpass API-a, za dati grad. Postoji mogućnost da su Overpass serveri zauzeti u određenom trenutku, ali u većini slučajeva podaci o restoranima se pribavljaju, i upisuju sve tri baze podataka.
Izgled stranice odmah nakon što su restorani upisani u baze, i za već postojeće restorane (novi restorani su u Nišu, dok su postojeći u Beogradu):
![Map1](TastyTrails/slike/tekpovucenirestorani.png)
![Map2](TastyTrails/slike/recommendedrestoranimapa.png)
