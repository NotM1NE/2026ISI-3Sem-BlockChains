# Mokomasis 256 bitų maišos generatorius – v0.1

Blokų grandinių technologijų 1 praktinė užduotis. Autorius: Jakub Rogoža.

Šis README aprašo v0.1 realizaciją ir pateiktą 2026-11-05 rezultatų rinkinį. v0.2 algoritmas čia nevertinamas. Funkcija skirta mokymuisi; šie eksperimentai neįrodo kriptografinio saugumo.

## Paleidimas

Reikia .NET 9 vykdymo aplinkos ir suderinamo SDK. Iš repozitorijos šaknies pereiti į `Blockchain`:

```powershell
cd Blockchain
dotnet build -c Release
dotnet run -c Release
dotnet run -c Release -- "test-data/konstitucija.txt"
```

Be argumento įvedama viena teksto eilutė. Su failo keliu skaičiuojama failo turinio, o ne jo pavadinimo maiša. Neperskaitomas failas praneša klaidą ir grąžina 1. Teksto režimas papildomai parodo UTF-8 baitus, jų HEX užrašą ir pradines būsenos pozicijas; galutinė maiša išvedama paskutinėje eilutėje.

## Įvestis ir išvestis

Tekstas funkcijoje `HashInputHelper.GetBytes` koduojamas UTF-8. `Console.ReadLine` neįtraukia Enter sukurto eilutės skirtuko. Tarpai nešalinami, registras nekeičiamas, Unicode nenormalizuojamas. Failas skaitomas `File.ReadAllBytes`: išsaugomi BOM ir tikslūs LF / CRLF baitai. Vienodai atrodantis tekstas gali turėti kitus baitus. `Ąžuolas 🌳` šiame bandyme užima 14 baitų. Dabartinis `Program.cs` aiškiai nenustato konsolės koduotės; įvedamų ne ASCII simbolių perskaitymas priklauso ir nuo terminalo aplinkos.

Būsena turi 8 `uint`: 8 × 32 = 256 bitų. Kiekvienas elementas formatuojamas `X8`, todėl galutinė maiša visada turi 64 didžiąsias HEX pozicijas, įskaitant pradinius nulius. HEX yra išvesties užrašas, ne atskiras maišymo ar šifravimo veiksmas.

Failas įkeliamas visas į atmintį. Praktinę dydžio ribą lemia atmintis ir .NET masyvų ribos. Srautinis failo apdorojimas nerealizuotas.

## Algoritmo principas

Pradinė būsena `[10,20,30,40,50,60,70,80]` kiekvienam kvietimui sukuriama iš naujo. Įvesties baitai nuosekliai keičia tą patį masyvą. Pradinė vieta cikliškai juda nuo 0 iki 7; pasiekus pabaigą grįžtama į 0, papildomos vietos nekuriamos.

```text
H ← [10,20,30,40,50,60,70,80]
start ← 0
kiekvienam įvesties baitui b:
    H[start] ← ROTL32((H[start] + b) × 7, 5)
    previous ← start
    kartoti 7 kartus:
        current ← (previous + 1) mod 8
        H[current] ← ROTL32(H[current] + 3 × H[previous], 11)
        previous ← current
    start ← (start + 1) mod 8
grąžinti H[0]…H[7] kaip X8 HEX žodžius
```

Rotacija pasuka 32 bitų žodžio bitus, išstumtus bitus grąžindama kitame gale. `unchecked` sudėtis ir daugyba vyksta modulo 2^32. Būsena atnaujinama vietoje: kiekvienas kitas elementas naudoja jau pakeistą ankstesnį, taip baito įtaka perduodama per visą būseną. Tai taip pat padaro skaičiavimą priklausomą nuo baitų tvarkos.

Konstantos 3, 7 ir rotacijos 5, 11 yra realizacijos pasirinkimai. Didesnis operacijų skaičius savaime nereiškia didesnio saugumo. v0.1 neturi papildomo galutinio maišymo. Tuščiai įvesčiai lieka pradinė būsena:

```text
0000000A000000140000001E00000028000000320000003C0000004600000050
```

Kiekvienam baitui atliekamas pastovus operacijų skaičius: laikas O(n), būsenos atmintis O(1). Visos įvesties įkėlimas sunaudoja O(n) atminties.

## Bandymų sąlygos ir atkūrimas

- SDK patikros metu: 10.0.401; rezultatų metaduomenyse runtime .NET 9.0.20.
- OS metaduomenyse: Microsoft Windows 10.0.26200; architektūra X64, 24 loginiai procesoriai.
- Konfigūracija Release. Laikmatis `Stopwatch`.
- Atsitiktinių įvesčių seed: 2026.
- Abėcėlė: `ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789` (62 ASCII simboliai).
- Spartos failas: `test-data/konstitucija.txt`, 76 384 baitai, 789 eilutės. Tai anksčiau projekte išsaugotas kurso failas; tiksli pirminė atsisiuntimo nuoroda neišlikusi.
- Lyginant būsimą versiją reikia išlaikyti tas pačias įvestis, jų ilgius, druską ir aplinką. Šio išplėsto rinkinio bendro vidurkio negalima tiesiog lyginti su ankstesniu tik keturių ilgių vidurkiu.

```powershell
dotnet run -c Release -- --test
dotnet run -c Release -- --test
dotnet run -c Release -- --performance
dotnet run -c Release -- --collisions
dotnet run -c Release -- --avalanche
dotnet run -c Release -- --guessing
```

Komandos vykdomos `Blockchain` aplanke, rezultatai rašomi į jo `results`. Paleidimai perrašo to eksperimento failus. Prieš keičiant algoritmą išsaugoti atskirą rezultatų kopiją; seno algoritmo baseline negalima naudoti naujo algoritmo teisingumui vertinti. Pateiktas rinkinys dokumentuojamas jo neperleidžiant iš naujo.

## 1–3. Įvestys, formatas ir determinizmas

Patikrintos 24 įvestys: tuščia, vieno baito `a` ir `b`, trumpas tekstas, pasikartojimai, `ab` / `ba`, tarpai, eilutės skirtukas, UTF-8 ir atsitiktinės 1024, 2048, 4096 baitų įvestys su vieno baito pakeitimais pradžioje, viduryje ir gale.

| Patikra | Rezultatas |
|---|---|
| 64 tinkami HEX simboliai | 24 / 24 |
| Pakartotinis funkcijos kvietimas | 24 / 24 |
| A → B → A | 24 / 24 |
| Atminties ir iš failo perskaitytų baitų maišos | 24 / 24 |
| `correctness.csv` sutampa su `correctness-baseline.csv` | Taip |

Pirmas `--test` paleidimas, jei baseline dar nėra, jį išsaugo; kitas palygina atskiro proceso rezultatą. `file_ok` patikra kviečia funkciją tiesiogiai ir nėra viso konsolės režimo integracinis bandymas. Dabartinio rinkinio atskiro CLI patikros žurnalo nepateikta. Rankiniam patikrinimui įvesti `a`, paskui vykdyti `dotnet run -c Release -- "test-data/correctness/one_byte_a.txt"` ir palyginti paskutines maišų eilutes. Šios patikros patvirtina nuoseklumą, bet ne saugumą.

## 4. Sparta

Imamos pirmos 1, 2, 4, 8… eilutės bei visas failas, išsaugant eilučių skirtukus. Failas ir ištraukos paruošiami prieš matavimą. Po 10 apšilimo skaičiavimų automatiškai parenkamas kartojimų skaičius ir atliekami 5 matavimai kiekvienam dydžiui. Matuojamas tik skaičiavimas; failų I/O ir konsolės išvedimas neįtraukti. Rezultatas panaudojamas kontrolinėje reikšmėje.

| Eilutės | Baitai | Matavimai | Min. ms | Vid. ms | Maks. ms |
|---|---|---|---|---|---|
| 1 | 71 | 5 | 0.000905 | 0.001755 | 0.002149 |
| 2 | 125 | 5 | 0.000420 | 0.000435 | 0.000442 |
| 4 | 209 | 5 | 0.000676 | 0.000692 | 0.000725 |
| 8 | 370 | 5 | 0.001186 | 0.001190 | 0.001194 |
| 16 | 1012 | 5 | 0.003223 | 0.003280 | 0.003383 |
| 32 | 1873 | 5 | 0.005955 | 0.005969 | 0.005987 |
| 64 | 3776 | 5 | 0.011952 | 0.012052 | 0.012201 |
| 128 | 9283 | 5 | 0.029353 | 0.029400 | 0.029430 |
| 256 | 20665 | 5 | 0.065460 | 0.065632 | 0.065825 |
| 512 | 47946 | 5 | 0.156036 | 0.156149 | 0.156331 |
| 789 | 76384 | 5 | 0.248870 | 0.249476 | 0.249791 |

<img width="1552" height="560" alt="image" src="https://github.com/user-attachments/assets/936e8f3e-c600-4d37-96c7-c62287dfcfe4" />

Didėjant įvesties dydžiui, laikas auga maždaug tiesiškai. Visas failas apskaičiuojamas vidutiniškai per 0,249476 ms, apie 306,18 MB/s. Pirmo dydžio laikas išsiskiria: 71 baitas vidutiniškai truko ilgiau nei 125 baitai. Galimos JIT optimizavimo ir sistemos apkrovos priežastys, tačiau šis bandymas neleidžia tiksliai nustatyti priežasties. Išsaugoti 55 pradiniai matavimai (`performance-raw.csv`).

## 5. Kolizijos

Kiekvienam iš devynių ilgių generuota po 100 000 porų: iš viso 900 000 porų. Privalomi ilgiai 10, 100, 500, 1000 sudaro 400 000 porų; 1, 2, 4, 8 ir 10 000 yra papildomi bandymai. Tikrinama kiekviena pora ir visas kiekvieno ilgio rinkinys. Pasikartojusi ta pati įvestis nėra kolizija.

| Ilgis | Poros | Sugeneruota | Skirtingų įvesčių | Porinės kolizijos | Viso rinkinio kolizijos | Kolizijų grupės |
|---|---|---|---|---|---|---|
| 1 | 100000 | 200000 | 62 | 0 | 0 | 0 |
| 2 | 100000 | 200000 | 3844 | 0 | 0 | 0 |
| 4 | 100000 | 200000 | 198591 | 0 | 0 | 0 |
| 8 | 100000 | 200000 | 200000 | 0 | 0 | 0 |
| 10 | 100000 | 200000 | 200000 | 0 | 0 | 0 |
| 100 | 100000 | 200000 | 200000 | 0 | 0 | 0 |
| 500 | 100000 | 200000 | 200000 | 0 | 0 | 0 |
| 1000 | 100000 | 200000 | 200000 | 0 | 0 | 0 |
| 10000 | 100000 | 200000 | 200000 | 0 | 0 | 0 |


Vieno simbolio atveju abėcėlėje yra tik 62 skirtingos įvestys, dviejų simbolių – 62² = 3844. Todėl tarp 200 000 sugeneruotų įvesčių daug kartojimų; tai nėra 200 000 skirtingų įvesčių ir jų kartojimasis nėra kolizija. Keturių simbolių atveju gauta 198 591 skirtinga įvestis.

Papildomai patikrintos 23 struktūruotos įvestys: perstatymai, pasikartojimai, tarpai, LF ir CRLF. `collision-structured-examples.csv` ir `collision-examples.csv` turi tik antraštes: šiame rinkinyje kolizijų nerasta. Tai nėra įrodymas, kad algoritmas atsparus kolizijoms. Fiksuoto 256 bitų ilgio funkcijai kolizijos neišvengiamos, nes galimų įvesčių daugiau nei 2^256 išvesčių.

## 6. Lavinos efektas

Kiekvienam iš devynių ilgių atlikta po 25 000 porų, iš viso 225 000. Kopijoje pakeistas vienas ASCII simbolis kitu tos pačios abėcėlės simboliu; tai nebūtinai vieno bito pakeitimas. HEX pirmiausia dekoduojamas į baitus. Bitų rodiklis = skirtingi bitai / 256 × 100; HEX rodiklis = skirtingos pozicijos / 64 × 100.

| Ilgis | Poros | Bitų min. % | Bitų maks. % | Bitų vid. % | HEX min. % | HEX maks. % | HEX vid. % |
|---|---|---|---|---|---|---|---|
| 1 | 25000 | 13.6719 | 35.5469 | 24.7459 | 32.8125 | 60.9375 | 49.6681 |
| 2 | 25000 | 12.1094 | 52.3438 | 31.2706 | 32.8125 | 87.5000 | 61.7119 |
| 4 | 25000 | 13.2812 | 62.8906 | 40.0421 | 32.8125 | 100.0000 | 76.6698 |
| 8 | 25000 | 13.6719 | 64.4531 | 45.0510 | 32.8125 | 100.0000 | 85.3122 |
| 10 | 25000 | 14.0625 | 62.5000 | 45.9420 | 34.3750 | 100.0000 | 86.7964 |
| 100 | 25000 | 14.8438 | 64.4531 | 49.6243 | 34.3750 | 100.0000 | 93.1100 |
| 500 | 25000 | 14.8438 | 62.1094 | 49.9137 | 34.3750 | 100.0000 | 93.6094 |
| 1000 | 25000 | 17.1875 | 63.2812 | 49.9444 | 37.5000 | 100.0000 | 93.6446 |
| 10000 | 25000 | 28.9062 | 62.8906 | 49.9762 | 54.6875 | 100.0000 | 93.7264 |
| all | 225000 | 12.1094 | 64.4531 | 42.9456 | 32.8125 | 100.0000 | 81.5832 |

**Svarbu:** `all` apima visus devynis ilgius. Privalomiems keturiems ilgiams atskirai yra 100 000 porų, jų bendras bitų vidurkis **48.8561 %**, HEX vidurkis **91.7901 %** (perskaičiuota iš pradinių matavimų).

<img width="1577" height="566" alt="image" src="https://github.com/user-attachments/assets/3a6d27d0-909b-49e7-8d50-adf5f2d85867" />

Trumpoms įvestims nustatytas silpnas maišymas: vieno simbolio vidurkis 24,7459 %, dviejų – 31,2706 %. 10 simbolių vidurkis 45,9420 %, o ilgoms įvestims artėja prie 50 %. Minimumai rodo, kad geras vidurkis neaprašo visų atvejų. Galima silpnumo priežastis – galutinio maišymo nebuvimas; tai hipotezė, ne atskirai įrodytas priežastinis ryšys.

50 % bitų ir 93,75 % HEX yra nepriklausomų tolygių išvesčių vidurkio orientyrai: bitas sutampa su tikimybe 1/2, HEX pozicija – 1/16. Nereikalaujama, kad kiekviena pora skirtųsi bent 50 %. Histogramoje naudoti 5 procentinių punktų intervalai; Excel atskirai pateiktos privalomų keturių ilgių ir visų devynių ilgių dažnių sumos.

## 7. PIN spėjimas ir vieša druska

Bandymo paruošimas pasirenka PIN `1234`, bet paieškos funkcija gauna tik tikslinę maišą ir, kai naudojama, viešą druską. Kiekvienas paleidimas patikrina visus `0000`–`9999` kandidatus, nestodamas ties pirmu sutapimu. Kiekvienam režimui atliekama 10 matuojamų paleidimų po 1000 apšilimo maišos kvietimų, kurie į laiką neįtraukiami.

| Režimas | Paleidimai | Kandidatai / paleidimą | Iš viso | Vid. ms | Min. ms | Maks. ms | Rastas PIN |
|---|---|---|---|---|---|---|---|
| without_salt | 10 | 10000 | 100000 | 6.736060 | 5.246100 | 15.680200 | 1234 |
| public_salt | 10 | 10000 | 100000 | 11.718130 | 11.438500 | 12.039900 | 1234 |

<img width="1727" height="358" alt="image" src="https://github.com/user-attachments/assets/41f1596d-e295-4d1d-a816-eb1324302d65" />

Vidurkis čia reiškia **vienos visos 10 000 kandidatų paieškos laiką**, ne vienos maišos laiką. Kiekvienas režimas iš viso atliko 100 000 matuojamų bandymų. Laikas apima kandidatų formatavimą, UTF-8 kodavimą, baitų sujungimą, maišą, palyginimą ir sutapimo registravimą. Išvedimas vyksta po laikmačio sustabdymo.

Druska yra 16 atsitiktinių baitų, išsaugotų `guessing-salt.hex`. HEX užrašas perskaitomas atgal į baitus; skaičiuojama `H(PIN baitai || druskos baitai)`, o ne PIN su HEX tekstu. Ta pati druska išlaikoma visiems kandidatams ir dešimčiai paleidimų. Paieška žino viešą druską ir randa `1234` abiem režimais. Šiame seanse ilgesnės įvesties režimas su druska lėtesnis, bet laiko skirtume yra ir vykdymo aplinkos įtaka. Be druskos pirmas matavimas 15,6802 ms ryškiai skiriasi nuo minimumo 5,2461 ms net po apšilimo; priežastis šiuo bandymu nenustatyta.

Vieša druska nesustabdo vieno taikinio mažo kandidatų rinkinio perrinkimo. Jos paskirtis – trukdyti vieną iš anksto parengtą maišų lentelę naudoti daugeliui taikinių su skirtingomis druskomis. Kelių taikinių bandymas šiame kode neatliktas. Maišos sutapimas apskritai neįrodo, kad atkurta vienintelė pradinė įvestis, nes galimos kolizijos.

Slaptas atsitiktinumas `r` konstrukcijoje `H(input || r)` reikštų, kad paieškoje nežinoma ir įvestis, ir `r`. Atskleidus abu galima perskaičiuoti maišą. Tai iliustruoja commitment idėją, tačiau neįrodo šios konstrukcijos slėpimo ar susiejimo saugumo. Slapto `r` paieška neatlikta.

Šis eksperimentas nėra iššifravimas ir nėra proof-of-work: čia ieškoma konkrečią maišą duodančios įvesties, o PoW ieškoma nonce, kurios maiša tenkina nustatytą sąlygą.

## 8. Išvados ir ribotumai

- Pateikti rezultatai patvirtina pastovų formatą ir determinizmą tirtoms įvestims.
- Spartos tendencija atitinka O(n), tačiau mažiausi matavimai turi anomalijų.
- Kolizijų tirtuose rinkiniuose nerasta. Tai neįrodo bendro atsparumo.
- Aptiktas silpnas labai trumpų įvesčių lavinos efektas. Galimas būsimas patobulinimas – galutinis būsenos maišymas; v0.2 rezultatai šiame README nepateikti.
- Mažas PIN kandidatų rinkinys lengvai perrenkamas ir su vieša druska.
- Blokų grandinėse maišos gali susieti blokus ir būti naudojamos Merkle struktūrose. Šiame darbe sukurta maišos funkcija ir bandymai, ne pilna blokų grandinė.

## DI pagalba ir šaltiniai

v0.1 algoritmo realizacija paimta iš autoriaus esamo projekto. Testų rengimui ir aiškinimui, 10 paleidimų vidurkio pakeitimui, rezultatų analizei, README ir Excel parengimui naudota ChatGPT / Codex (GPT-6) pagalba. Šis aprašymas neteigia, kad visas pateiktas kodas sukurtas be DI; ankstesnę algoritmo kūrimo istoriją reikia vertinti pagal faktinius projekto įrašus.

Svarbios užklausos: paaiškinti algoritmo ciklus ir bandymus; PIN paiešką kartoti 10 kartų ir imti vidurkį; parengti v0.1 README, lenteles ir grafikus iš pateiktų rezultatų. Priimta: atskirti pradines reikšmes nuo suvestinių ir privalomus ilgius nuo papildomų. Atmesta interpretacija, kad druska skirta pagreitinti atspėjimą arba kad nerastos kolizijos įrodo saugumą. Skaičiai tikrinti su CSV, pradine lavinos lentele ir teisingumo baseline.

Šaltiniai: VU 2026 m. užduotis „Sukurk savo maišos generatorių“ ir kontrolinis sąrašas; esamas projekto kodas; pateikti `results/*.csv` ir `*-metadata.txt`; projekte išsaugotas kurso konstitucijos failas. Prasmingų commit, istorinio darbo be DI ir viešo GitHub pateikimo faktų vien šis README nepatvirtina.
