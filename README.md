**2026ISI-3Sem-BlockChains**

# Mokomasis 256 bitų maišos generatorius

Blokų grandinių technologijų 1-oji praktinė užduotis. Autorius: Jakub Rogoža.

Projektas parašytas C#/.NET. Sukurta sava maišos funkcija, kuri kintamo ilgio baitų seką paverčia 256 bitų reikšme, išvedama 64 didžiosiomis HEX raidėmis ir skaitmenimis. Tai mokomasis algoritmas; atlikti eksperimentai neįrodo kriptografinio saugumo.

## Paleidimas

Komandas vykdyti kataloge, kuriame yra projekto `.csproj` failas. Projektas skirtas .NET 9; tikslų įdiegtos SDK versijos numerį prieš pateikiant darbą reikia papildyti pagal `dotnet --info`.

```bash
dotnet build -c Release
dotnet run -c Release
dotnet run -c Release -- "test-data/konstitucija.txt"
dotnet run -c Release -- --test
dotnet run -c Release -- --test
dotnet run -c Release -- --avalanche
dotnet run -c Release -- --collisions
dotnet run -c Release -- --performance
dotnet run -c Release -- --guessing
```

Be argumentų programa prašo vienos teksto eilutės. Su failo keliu apskaičiuojama failo turinio maiša. Neperskaitomas failas sukelia klaidos pranešimą, o ne tyliai laikomas tuščia įvestimi. `--test` paleidžiamas du kartus, kad būtų patikrinti rezultatai tarp atskirų programos paleidimų.

## Įvestis ir išvestis

Tekstas koduojamas UTF-8. `Console.ReadLine()` neįtraukia įvedimą užbaigiančios eilutės pabaigos. Tarpai nešalinami, raidžių registras nekeičiamas ir Unicode tekstas nenormalizuojamas. Failas skaitomas kaip tikslūs baitai, įskaitant eilučių skirtukus ir BOM, jei jis yra. LF ir CRLF todėl gali duoti skirtingas maišas.

UTF-8 simbolių ir baitų skaičius gali skirtis: `Ąžuolas 🌳` šiame teste užima 14 baitų. Atsitiktinių bandymų abėcėlė yra ASCII, todėl ten simbolių ir baitų skaičiai sutampa.

Būsena sudaryta iš 8 `uint` elementų: 8 × 32 = 256 bitai = 32 baitai. Kiekvienas elementas formatuojamas `X8`, todėl išsaugomi pradiniai nuliai, o bendras rezultatas yra 64 HEX simboliai.

Failai įkeliami visi iš karto (`File.ReadAllBytes`); praktinis dydžio apribojimas priklauso nuo atminties ir .NET masyvų ribų. Srautinis didelių failų apdorojimas nerealizuotas. Rankinis režimas priima vieną eilutę.

## Algoritmas

Pradinė būsena: `[10, 20, 30, 40, 50, 60, 70, 80]`. Kiekvienam baitui parenkamas vienas elementas, prie jo pridedamas baitas, rezultatas dauginamas iš 7 ir pasukamas 5 bitais į kairę. Toliau cikliškai atnaujinami likę 7 elementai: prie dabartinio pridedama ankstesnio elemento reikšmė, padauginta iš 3, ir rezultatas pasukamas 11 bitų į kairę.

```text
H ← [10, 20, 30, 40, 50, 60, 70, 80]
start ← 0
Kiekvienam įvesties baitui b:
    H[start] ← ROTL32((H[start] + b) × 7, 5)
    previous ← start
    Kartoti 7 kartus:
        current ← (previous + 1) mod 8
        H[current] ← ROTL32(H[current] + H[previous] × 3, 11)
        previous ← current
    start ← (start + 1) mod 8
Grąžinti H kaip 64 HEX simbolius
```

Aritmetika vykdoma `unchecked`: perviršis apvyniojamas modulo 2^32. Rotacija išsaugo bitus, perkeldama iš kairės išstumtus bitus į dešinę. Atnaujinimai vyksta vietoje: vėlesnis elementas naudoja jau atnaujintą ankstesnį elementą. Taip įvesties pakeitimas gali paveikti visą būseną. Konstantos 3, 7 ir rotacijų dydžiai yra projektavimo pasirinkimai, o ne saugumo garantijos.

Tuščiai įvesčiai baitų ciklas nevyksta, todėl gaunama pradinė būsena:

```text
0000000A000000140000001E00000028000000320000003C0000004600000050
```

Kiekvienam baitui atliekamas fiksuotas operacijų skaičius: skaičiavimo laikas O(n), būsenos atmintis O(1). Įvesties įkėlimas reikalauja O(n) atminties. Galutinis papildomas maišymas šiame variante nepridėtas.

## Bandymų aplinka ir atkūrimas

| Parametras | Reikšmė |
|---|---|
| OS | Windows 11 Pro |
| CPU | AMD Ryzen 9 9900X, 12 branduolių |
| RAM | 32 GB DDR5, 6000 MT/s |
| Diskas | Samsung SSD 990 PRO 2 TB |
| GPU | NVIDIA GeForce RTX 5070 Ti, 16 GB; bandymuose nenaudojama |
| Projektas | C# / .NET 9; tikslų SDK ir runtime numerį papildyti |
| Konfigūracija | Release |
| Atsitiktinių įvesčių seed | 2026 |
| Abėcėlė | `ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789` |

Tikslią vykdymo aplinką fiksuoja eksperimentų `*-metadata.txt` failai. Versijų palyginimui išsaugoti tą patį duomenų rinkinį, konstitucijos failą ir druską. Vien seed negarantuoja identiškos sekos tarp visų skirtingų .NET realizacijų.

## 1–3 eksperimentai: įvestys, formatas ir determinizmas

Patikrintos 24 įvestys: tuščia, vieno baito `a` ir `b`, trumpas tekstas, pasikartojantys simboliai ir šablonai, UTF-8 pavyzdys, tarpai, nauja eilutė, `ab` / `ba`, atsitiktinės 1024, 2048 ir 4096 baitų sekos bei jų kopijos su vienu pakeistu baitu pradžioje, viduryje ir gale.

| Patikra | Rezultatas |
|---|---|
| 64 tinkami HEX simboliai | 24 / 24 PASS |
| Pakartotiniai tos pačios įvesties skaičiavimai | 24 / 24 PASS |
| A → B → A seka | 24 / 24 PASS |
| Baitai atmintyje ir perskaityti iš failo | 24 / 24 PASS |
| Palyginimas su ankstesnio programos paleidimo baseline | PASS |

Automatinė failo patikra lygina tiesioginius funkcijos kvietimus su tais pačiais baitais. Tikro rankinio CLI ir failo režimų integracinę patikrą reikia atskirai užfiksuoti. PASS nepatvirtina atsparumo kolizijoms ar gero lavinos efekto.

## 4 eksperimentas: sparta

Naudotas kurso `konstitucija.txt`: 789 eilutės, 76 384 baitai. Imtos pradžios ištraukos su 1, 2, 4, 8, … eilutėmis bei visas failas, išsaugant eilučių skirtukus. Failas ir ištraukos paruošti prieš matavimą. Matuojama `Stopwatch`, be failų I/O ir išvedimo į konsolę. Po 10 apšilimo kvietimų parenkami grupiniai kartojimai, tada atliekami 5 matavimai kiekvienam dydžiui. Skaičiavimo rezultatai panaudojami kontrolinėje reikšmėje.

| Eilutės | Baitai | Kartojimai matavime | Min. ms / hash | Vid. ms / hash | Maks. ms / hash |
|---:|---:|---:|---:|---:|---:|
| 1 | 71 | 32768 | 0.000292 | 0.001574 | 0.002121 |
| 2 | 125 | 131072 | 0.000472 | 0.000488 | 0.000496 |
| 4 | 209 | 65536 | 0.000796 | 0.000806 | 0.000819 |
| 8 | 370 | 65536 | 0.001318 | 0.001395 | 0.001459 |
| 16 | 1012 | 16384 | 0.003742 | 0.003850 | 0.003941 |
| 32 | 1873 | 8192 | 0.006511 | 0.007093 | 0.007358 |
| 64 | 3776 | 4096 | 0.014631 | 0.014672 | 0.014718 |
| 128 | 9283 | 2048 | 0.034961 | 0.035772 | 0.036053 |
| 256 | 20665 | 1024 | 0.075985 | 0.077780 | 0.079557 |
| 512 | 47946 | 512 | 0.167027 | 0.174338 | 0.179413 |
| 789 | 76384 | 256 | 0.274397 | 0.284502 | 0.292846 |


Laikas didėja maždaug tiesiškai didėjant baitų skaičiui. Daugumoje dydžių našumas apie 257–275 MB/s. Pirmas matavimas išsiskiria dideliu vidurkiu ir sklaida; galimos vykdymo optimizavimo ar sistemos apkrovos priežastys, bet šie matavimai neleidžia nustatyti tikslios priežasties.

## 5 eksperimentas: kolizijos

Kiekvienam ASCII ilgiui 10, 100, 500 ir 1000 generuota po 100 000 porų. Kiekvienos poros tekstai skirtingi. Tikrinti porų hash sutapimai ir bendri sutapimai visame konkretaus ilgio rinkinyje. Pakartota identiška įvestis nelaikoma kolizija.

| Ilgis | Poros | Sugeneruotos įvestys | Porinės kolizijos | Bendri sutapimai tarp skirtingų įvesčių |
|---:|---:|---:|---:|---:|
| 10 | 100000 | 200000 | 0 | 0 |
| 100 | 100000 | 200000 | 0 | 0 |
| 500 | 100000 | 200000 | 0 | 0 |
| 1000 | 100000 | 200000 | 0 | 0 |

Tikslūs skirtingų įvesčių ir kolizijų grupių skaičiai saugomi originaliame `collision-summary.csv`. Papildomai palygintos 23 struktūruotos įvestys: simbolių perstatymai, pasikartojimai, šablonai, tarpai ir LF / CRLF. Rasta 0 kolizinių porų ir 0 grupių. Todėl pavyzdžių failai turi tik antraštes.

Idealios 256 bitų maišos atveju vienos nesusijusių įvesčių poros kolizijos tikimybė apie 2^-256. Rinkinyje yra m(m−1)/2 galimų porų. Net 200 000 skirtingų įvesčių rinkinyje kolizija idealiai maišai būtų labai mažai tikėtina. Nerastos kolizijos nėra šio algoritmo kriptografinio atsparumo įrodymas.

## 6 eksperimentas: lavinos efektas

Generuota po 25 000 porų kiekvienam ilgiui, iš viso 100 000. Kopijoje pakeistas tiksliai vienas atsitiktinis ASCII simbolis kitu tos pačios abėcėlės simboliu. Vienas simbolio pakeitimas gali pakeisti daugiau nei vieną įvesties bitą.

Bitų skirtumas = 100 × skirtingų bitų skaičius / 256. HEX skirtumas = 100 × skirtingų HEX pozicijų skaičius / 64. HEX dekoduotas į baitus prieš XOR ir bitų skaičiavimą.

| Ilgis | Bitų min. % | Bitų maks. % | Bitų vid. % | HEX min. % | HEX maks. % | HEX vid. % |
|---:|---:|---:|---:|---:|---:|---:|
| 10 | 12.8906 | 62.1094 | 46.0729 | 32.8125 | 100.0000 | 86.9915 |
| 100 | 15.2344 | 64.0625 | 49.6200 | 35.9375 | 100.0000 | 93.0658 |
| 500 | 16.4062 | 62.5000 | 49.9264 | 35.9375 | 100.0000 | 93.6305 |
| 1000 | 17.1875 | 63.6719 | 49.9774 | 39.0625 | 100.0000 | 93.7135 |
| Visi | 12.8906 | 64.0625 | 48.8992 | 32.8125 | 100.0000 | 91.8503 |


Horizontalioje ašyje – pasikeitusių hash bitų procentas, vertikalioje – porų skaičius. Grafike matavimai sugrupuoti į 5 procentinių punktų intervalus; tikslūs skaičiai lieka CSV. 50 % linija yra statistinis orientyras, o ne kiekvienos poros reikalavimas.

Ilgų įvesčių vidurkiai artimi 50 % bitams ir 93.75 % HEX. Trumpų įvesčių maišymas silpnesnis. Mažiau nei 25 % bitų pasikeitė 1140, 123, 24 ir 6 porose atitinkamai keturiems ilgiams. Geri vidurkiai slepia silpnų atvejų uodegą. Galima priežastis – paskutinio baito pakeitimui nelieka vėlesnių įvesties maišymo žingsnių; tai hipotezė, kurią tikrintų atskira analizė pagal pakeitimo poziciją.

Geras lavinos efektas gali egzistuoti kartu su lengvai randamomis kolizijomis: jis matuoja pakeitimo pasklidimą, o ne visą saugumą. Struktūruoti ir algoritmui pritaikyti kolizijų bandymai gali atskleisti silpnybes, kurių vidurkiai nerodo.

## 7 eksperimentas: kandidatų perrinkimas ir druska

Paruošimui pasirinktas PIN `2026`. Paieškos funkcijai perduodamas tik tikslinis hash ir, kai taikoma, vieša druska. Tikrinami visi keturženkliai ASCII kandidatai `0000`–`9999`, nestabdant po pirmo sutapimo.

| Režimas | Bandymai | Laikas ms | Sutampantys kandidatai |
|---|---:|---:|---|
| Be druskos | 10000 | 13.869800 | 2026 |
| Su vieša druska | 10000 | 12.947100 | 2026 |

Druska – 16 atsitiktinių baitų, sugeneruotų `RandomNumberGenerator`, saugomų HEX faile. Hash skaičiuojamas iš UTF-8 PIN baitų ir žalių druskos baitų sujungimo `H(input || salt)`, ne iš druskos HEX teksto. Vienam taikiniui druska fiksuota. Matavimas apima kandidatų formatavimą, kodavimą, sujungimą, hash skaičiavimą ir palyginimą, bet ne rezultatų išvedimą. Prieš matavimą atliktas apšilimas.

Vieša druska nesustabdo vieno taikinio mažo kandidatų rinkinio perrinkimo. Tačiau skirtingiems taikiniams su skirtingomis druskomis negalima tiesiog panaudoti vienos iš anksto paruoštos hash lentelės: kiekvienai druskai skaičiuojama atskirai. Kelių taikinių matavimas šiame kode neatliktas; tai konceptualus paaiškinimas. Vienas laiko matavimas neįrodo, kad druska skaičiavimą spartina.

Šiame rinkinyje sutapo vienintelis kandidatas, tačiau apskritai hash sutapimas nebūtinai identifikuoja pradinę įvestį dėl galimų kolizijų.

Kai `r` slaptas, `H(input || r)` paieškoje nežinomas ir kandidatas, ir `r`. Pavyzdžiui, 16 tolygiai atsitiktinių baitų turi 2^128 galimų reikšmių; tai kombinatorinis dydis, ne šio algoritmo saugumo įrodymas. Atskleidus pranešimą ir `r`, galima perskaičiuoti hash ir patikrinti sutapimą. Tai iliustruoja commitment idėją, tačiau neįrodo slėpimo ar negalėjimo pakeisti pranešimą. Slapto `r` didelės erdvės perrinkimas neatliktas ir užduotyje nereikalaujamas.

Tai nėra proof-of-work: čia ieškoma konkrečios maišos pirmavaizdžio tarp kandidatų, o ne nonce, tenkinančios tikslinę sąlygą.

## 8 eksperimentas: išvados, versijos ir DI

Atliktos patikros parodė nuoseklų formatą ir determinizmą, maždaug tiesinę spartą, nerastas kolizijas tirtuose rinkiniuose bei silpnesnį trumpų įvesčių lavinos efektą. Geri ilgesnių įvesčių vidurkiai neįrodo atsparumo pirmavaizdžių paieškai ar kolizijoms. Mažas viešas kandidatų rinkinys perrenkamas greitai.

### Versijų istorija – papildyti prieš pateikimą

Ši ataskaita aprašo vieną pateiktą ir išbandytą algoritmo realizaciją. Git tagai, commit identifikatoriai, savarankiškų patobulinimų istorija ir realus v0.1 / v0.2 palyginimas čia nepatvirtinti. Prieš pateikimą įrašyti faktines versijas ir jų rezultatus. Nežymėti DI pagalba sukurto kodo kaip sukurto be DI. Vienas testuotas variantas nepakeičia užduotyje reikalaujamo versijų palyginimo.

### DI pagalba

Naudota ChatGPT pagalba testų aiškinimui, testavimo kodo rengimui / papildymui, rezultatų interpretavimui, README ir grafikų parengimui. Papildyti tikslų naudotą modelį ir algoritmo kūrimo pagalbos istoriją pagal faktinį darbą.

Svarbios užklausos: paaiškinti testus ir trūkstamus reikalavimus; pasiūlyti lavinos pagerinimą; pridėti struktūruotus kolizijų bandymus; parengti kandidatų perrinkimo su druska bandymą; parengti README ir grafikus.

Priimti struktūruotų kolizijų ir kandidatų perrinkimo testų pasiūlymai. Jų vykdymo rezultatai peržiūrėti: 23 struktūruotos įvestys be kolizijų; abu perrinkimai rado `2026`. Pasiūlytas galutinis papildomas algoritmo maišymas neįgyvendintas. Jo pagerėjimas nebuvo išmatuotas; šio pakeitimo rezultatai nenurodomi.

## Rezultatų failai

Prie projekto išsaugoti originalius `results/*.csv`, `results/*-metadata.txt`, `results/guessing-salt.hex`, `test-data/correctness/*` ir kurso `test-data/konstitucija.txt`. Šis parengtas paketas turi grafikams naudotus spartos ir histogramos duomenis; jis nepakeičia visų originalių matavimų failų.

## Šaltiniai

- VU kurso „Blokų grandinių technologijos“ 2026 m. 1-osios praktinės užduoties formuluotė „Sukurk savo maišos generatorių“ ir kontrolinis sąrašas.
- Kurso pateiktas `konstitucija.txt`; tikslią kurso atsisiuntimo nuorodą papildyti.
- Pateiktas projekto kodas ir lokaliai atliktų bandymų rezultatai.
- ChatGPT pagalba, aprašyta DI naudojimo skyriuje.
