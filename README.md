# Mokomasis 256 bitų maišos generatorius: v0.1 ir v0.2

Autorius: Jakub Rogoža. VU blokų grandinių technologijų 1 praktinė užduotis.

v0.1 yra pradinė projekto maišos realizacija. v0.2 yra jos patobulinimas su DI pagalba. Ši analizė lygina abi versijas **vienodomis sąlygomis**, naudojant išplėstą devynių ilgių rinkinį. Tai mokomasis algoritmas, ne saugi slaptažodžių ar kitų realių sistemų apsaugos priemonė.

## Paleidimas

Reikia .NET 9 vykdymo aplinkos ir suderinamo SDK. v0.1 ir v0.2 lentelės žemiau aprašo anksčiau atliktą ir išsaugotą bendrą palyginimą.

Komandos iš repozitorijos šaknies:

```powershell
dotnet build Blockchain -c Release
dotnet run --project Blockchain -c Release
dotnet run --project Blockchain -c Release -- "Blockchain/test-data/konstitucija.txt"
```

Be failo kelio įvedama viena teksto eilutė; su keliu maiša skaičiuojama iš failo turinio. Neperskaitomas failas praneša klaidą ir grąžina 1.

### Dabartinės v0.2 bandymai

Komandas vykdyti `Blockchain` aplanke, kad būtų naudojami teisingi santykiniai `test-data` ir `results` keliai:

```powershell
cd Blockchain
dotnet run -c Release -- --test
dotnet run -c Release -- --test
dotnet run -c Release -- --performance
dotnet run -c Release -- --collisions
dotnet run -c Release -- --avalanche
dotnet run -c Release -- --guessing
```

Jeigu `results/correctness-baseline.csv` liko iš v0.1, prieš pirmą v0.2 patikrą jį išsaugoti kitu pavadinimu, pavyzdžiui, `correctness-baseline-v01.csv`. Pirmas paleidimas be baseline sukuria dabartinio algoritmo etaloną, antras palygina atskiro paleidimo rezultatą. Baseline keitimas pagrįstas tik sąmoningai pakeitus algoritmą, ne slepiant netikėtą nesutapimą. v0.1 ir v0.2 maišos neprivalo sutapti.

Paleidimai perrašo atitinkamo eksperimento failus darbiniame `Blockchain/results`. Prieš naują matavimą išsaugoti ankstesnius duomenis, jeigu jie reikalingi palyginimui.

### Išsaugotas versijų palyginimas

Šio README ir Excel abiejų versijų palyginimo šaltinis yra **`comparison-final`**. Jo duomenys gauti prieš pašalinant versijų pasirinkimą; algoritmo skaičiavimas nuo to nepasikeitė. Senasis `comparison` yra ankstesnis kitų imčių seansas, todėl su galutiniais rezultatais nemaišomas.

Senas `Compare-Versions.ps1` scenarijus rėmėsi pašalintu versijų pasirinkimu ir su dabartiniu paleidimu nebesuderinamas. Dabartinėms patikroms naudoti aukščiau pateiktas komandas. Naujam abiejų algoritmų palyginimui reikia atskirai vykdyti jų atitinkamas Git versijas, išlaikant vienodus testus, įvestis ir druską; jų rezultatų nesaugoti viename baseline aplanke.

## Įvestis ir išvestis

Tekstas koduojamas UTF-8; konsolės įvesties ir išvesties koduotė taip pat nustatyta UTF-8. `ReadLine` neįtraukia Enter eilutės pabaigos. Tarpai nešalinami, raidžių registras nekeičiamas, Unicode nenormalizuojamas. Failas skaitomas kaip tikslūs baitai, įskaitant BOM ir LF / CRLF. Todėl vizualiai panašūs tekstai gali skirtis baitais. `Ąžuolas 🌳` užima 14 UTF-8 baitų.

8 `uint` × 32 bitai = 256 bitų būsena. Kiekvieną žodį išvedant `X8` formatu gaunamos 64 didžiosios HEX pozicijos su pradiniais nuliais. HEX yra rezultato užrašas, ne šifravimas. Rankinis režimas priima vieną eilutę. `File.ReadAllBytes` įkelia visą failą; praktinė riba priklauso nuo atminties ir .NET masyvų ribų, srautinio apdorojimo nėra.

## v0.1 principas

Kiekvienam funkcijos kvietimui sukuriama nauja būsena `[10,20,30,40,50,60,70,80]`. Kiekvienas baitas keičia pradžios elementą, o jo poveikis perduodamas per likusius septynis. Pradžios pozicija cikliškai juda; pasiekus aštuntą grįžtama prie pirmos. Papildomų būsenos elementų nekuriama.

```text
H ← [10,20,30,40,50,60,70,80]; start ← 0
kiekvienam baitui b:
    H[start] ← ROTL32((H[start] + b) × 7, 5)
    previous ← start
    kartoti 7 kartus:
        current ← (previous + 1) mod 8
        H[current] ← ROTL32(H[current] + 3 × H[previous], 11)
        previous ← current
    start ← (start + 1) mod 8
grąžinti aštuonis H žodžius
```

Atnaujinama vietoje: kitas elementas naudoja jau atnaujintą ankstesnį. Sudėtis ir daugyba su `unchecked` apvyniojama modulo 2^32. Rotacija grąžina išstumtus bitus kitame 32 bitų žodžio gale. Konstantos ir rotacijų dydžiai yra projektavimo pasirinkimai, ne saugumo garantijos. Tuščia v0.1 įvestis grąžina pradinę būseną, užrašytą HEX. Galutinio papildomo maišymo nėra.

## v0.2: ką pakeitė DI pasiūlymas

Trumpai: pirmiausia vykdomas pradinis autoriaus maišymas, tada pridedama įvesties ilgio įtaka ir visa būsena dar keturis kartus permaišoma. Kiekviena vieta susiejama su kita ir per keturias vietas nutolusia vieta. Po papildomo bitų maišymo reikšmė įrašoma atgal. Būsena lieka aštuonių skaičių, išvestis – 64 HEX simboliai.

Kad kodą būtų lengviau skaityti, tos pačios konstantos užrašytos dešimtainiu formatu ir pavadintos:

| Pavadinimas | Reikšmė | Paskirtis |
|---|---:|---|
| MixingOffset | 2654435769 | Pridedama maišant būsenos vietas |
| FirstMultiplier | 2246822507 | Pirmoji papildomo bitų maišymo daugyba |
| SecondMultiplier | 3266489909 | Antroji papildomo bitų maišymo daugyba |

Pabraukimai skaičiuose tik atskiria skaitmenų grupes; `u` reiškia `uint` skaičių be ženklo. Tai fiksuoti algoritmo parametrai, ne slapti raktai. Pakeistas tik užrašas ir komentarai, ne skaičių reikšmės: visos 24 patikrintos v0.2 maišos liko identiškos ankstesniam palyginimui.

Pirmiausia vykdoma nepakeista `ComputeV01(input)`. Tai vidinis v0.2 skaičiavimo etapas, naudojantis pradinį maišymą, o ne vartotojui skirtas versijos pasirinkimas; ši funkcija turi likti kode. Tada į pirmus du žodžius XOR būdu įtraukiamos apatinės ir viršutinės 64 bitų baitų ilgio dalys. Po to atliekami keturi ciklai po aštuonis būsenos atnaujinimus.

```text
H ← v0.1(input)
H[0] ← H[0] XOR apatiniai32(input ilgis baitais)
H[1] ← H[1] XOR viršutiniai32(input ilgis baitais)
round nuo 0 iki 3:
    i nuo 0 iki 7:
        next ← H[(i+1) mod 8]
        opposite ← H[(i+4) mod 8]
        x ← H[i] + ROTL32(next,7) + MixingOffset + round
        x ← x XOR ROTL32(opposite,13)
        x ← (x XOR (x >> 16)) × FirstMultiplier
        x ← (x XOR (x >> 13)) × SecondMultiplier
        H[i] ← x XOR (x >> 16)
grąžinti H
```

Visi atnaujinimai 32 bitų, vyksta vietoje. Tikslas – paskutinio baito poveikiui suteikti papildomų pasklidimo žingsnių, o ilgį padaryti aiškia galutinio skaičiavimo dalimi. Keturi ciklai yra eksperimentinis pasirinkimas, neįrodyta optimali reikšmė.

XOR–poslinkių–daugybos blokas su `FirstMultiplier`, `SecondMultiplier` yra Austin Appleby **MurmurHash3 `fmix32`** (public domain) dalis: [pirminis kodas](https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp). Jo autorystė nepriskiriama šiam darbui. Visa MurmurHash3 funkcija nekopijuojama ir neiškviečiama; žinomas maišymo blokas naudojamas aštuonių žodžių būsenos papildomame maišyme.

Abiejų versijų laikas O(n), būsena O(1), visas įvesties įkėlimas O(n) atminties. v0.2 papildomai atlieka fiksuotus 32 būsenos žodžių atnaujinimus.

## Vienodos bandymų sąlygos

2026-11-05 naujas bendras seansas: SDK 10.0.401, runtime .NET 9.0.20, Windows metaduomenyse `Microsoft Windows 10.0.26200`, X64, 24 loginiai procesoriai, Release. Abiem versijoms tas pats kodas generuoja įvestis, seed 2026, abėcėlė `ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789`.

Privalomi ilgiai: **10, 100, 500, 1000**. Papildomi: **1, 2, 4, 8, 10 000**. Lavinai po 25 000 porų ilgiui: privaloma imtis 100 000, išplėsta 225 000 porų kiekvienai versijai. Kolizijoms po 100 000 porų ilgiui: 900 000 porų / versiją. PIN paieškai tas pats `1234`, ta pati išsaugota 16 baitų vieša druska ir po 10 matavimų kiekvienam režimui. Konstitucijos failas identiškas: 76 384 baitai, 789 eilutės.

## 1–3. Teisingumas ir determinizmas

24 atvejai: tuščia įvestis, vieno baito, trumpas tekstas, UTF-8, tarpai, eilutės skirtukas, tvarkos pakeitimai, pasikartojimai, 1024 / 2048 / 4096 baitų atsitiktinės įvestys ir jų vieno baito pakeitimai pradžioje, viduryje bei gale.

| Patikra | v0.1 | v0.2 |
|---|---:|---:|
| 64 tinkami HEX simboliai | 24 / 24 | 24 / 24 |
| Pakartotiniai funkcijos kvietimai | 24 / 24 | 24 / 24 |
| A → B → A | 24 / 24 | 24 / 24 |
| Atminties ir failo baitų sutapimas | 24 / 24 | 24 / 24 |
| Du atskiri paleidimai | Sutampa | Sutampa |
| Tikra CLI: tuščia, ASCII, UTF-8, tarpai pradžioje / gale | 5 / 5 | 5 / 5 |

CLI palygina konsolės ir failo rezultatą tiems patiems baitams; žurnalas `comparison-final/integration-checks.json`. v0.1 24 rezultatai sutapo su autoriaus pateiktu ankstesniu `correctness.csv`. Tai patvirtina, kad palyginime išsaugotas ankstesnis algoritmas. PASS nėra saugumo įrodymas.

## 4. Spartos palyginimas

Imamos pirmos 1, 2, 4, 8… eilutės ir visas failas. Baitai paruošiami prieš laikmatį. Po 10 apšilimo kvietimų parenkami grupiniai kartojimai ir atliekami 5 matavimai dydžiui; lentelėje vienos maišos laikas. I/O ir konsolės išvedimas nematuojami, rezultatas panaudojamas kontrolinėje reikšmėje. Kartojimų skaičiai ir 55 pradiniai matavimai / versiją išsaugoti CSV bei Excel.

| Baitai | v0.1 min. ms | v0.1 vid. ms | v0.1 maks. ms | v0.2 min. ms | v0.2 vid. ms | v0.2 maks. ms |
|---|---|---|---|---|---|---|
| 71 | 0.001370 | 0.001433 | 0.001600 | 0.001498 | 0.001551 | 0.001718 |
| 125 | 0.000489 | 0.000631 | 0.001191 | 0.000500 | 0.001044 | 0.002618 |
| 209 | 0.000786 | 0.000792 | 0.000811 | 0.000777 | 0.000783 | 0.000789 |
| 370 | 0.001424 | 0.001432 | 0.001435 | 0.001288 | 0.001293 | 0.001299 |
| 1012 | 0.003893 | 0.003898 | 0.003900 | 0.003307 | 0.003320 | 0.003347 |
| 1873 | 0.007195 | 0.007209 | 0.007220 | 0.006025 | 0.006032 | 0.006038 |
| 3776 | 0.013709 | 0.014364 | 0.014545 | 0.012027 | 0.012152 | 0.012295 |
| 9283 | 0.032692 | 0.034521 | 0.036047 | 0.029423 | 0.029482 | 0.029625 |
| 20665 | 0.074468 | 0.078361 | 0.079568 | 0.065449 | 0.065699 | 0.065930 |
| 47946 | 0.173087 | 0.181718 | 0.185395 | 0.151750 | 0.151978 | 0.152384 |
| 76384 | 0.281861 | 0.289518 | 0.293961 | 0.241027 | 0.241341 | 0.241636 |


<img width="1627" height="566" alt="image" src="https://github.com/user-attachments/assets/722c4882-305f-440d-8b54-cea47b962e6d" />

Laikas didėja maždaug tiesiškai. Visam failui v0.1 vidurkis 0,289518 ms, v0.2 0,241341 ms šiame seanse, tačiau tai **neįrodo**, kad papildomos operacijos spartina algoritmą: min./maks. intervalai čia nesusikerta, bet versijos matuotos nuosekliai viename procese kiekvienai versijai, todėl galima JIT / sistemos apkrovos įtaka. Mažų įvesčių anomalijos taip pat išlieka. Struktūriškai v0.2 turi papildomą fiksuotą darbą; patikimam greičio pranašumo teiginiui reikėtų atskirai valdyto ir kartojamo matavimo. Spartos skirtumo nepriskiriame saugumo pagerėjimui.

## 5. Kolizijos

Tikrinamos poros ir visas kiekvieno ilgio sugeneruotų įvesčių rinkinys. Kolizija skaičiuojama tik skirtingoms įvestims, o ne tai pačiai įvesčiai pasikartojus.

| Ilgis | Poros / versiją | Skirtingų įvesčių / versiją | v0.1 porinės / viso rinkinio / grupės | v0.2 porinės / viso rinkinio / grupės |
|---|---|---|---|---|
| 1 | 100000 | 62 | 0 / 0 / 0 | 0 / 0 / 0 |
| 2 | 100000 | 3844 | 0 / 0 / 0 | 0 / 0 / 0 |
| 4 | 100000 | 198591 | 0 / 0 / 0 | 0 / 0 / 0 |
| 8 | 100000 | 200000 | 0 / 0 / 0 | 0 / 0 / 0 |
| 10 | 100000 | 200000 | 0 / 0 / 0 | 0 / 0 / 0 |
| 100 | 100000 | 200000 | 0 / 0 / 0 | 0 / 0 / 0 |
| 500 | 100000 | 200000 | 0 / 0 / 0 | 0 / 0 / 0 |
| 1000 | 100000 | 200000 | 0 / 0 / 0 | 0 / 0 / 0 |
| 10000 | 100000 | 200000 | 0 / 0 / 0 | 0 / 0 / 0 |


Vieno simbolio galimų įvesčių tik 62, dviejų – 3844. Tai paaiškina didelį kartojimąsi; 200 000 sugeneruotų įvesčių nėra tiek pat skirtingų įvesčių. Papildomi 23 struktūruoti atvejai (perstatymai, šablonai, tarpai, LF / CRLF) abiem versijoms kolizijų taip pat neparodė. Pavyzdžių CSV turi tik antraštes.

**Išvada:** abiem versijoms tirtuose rinkiniuose kolizijų nerasta. Pagerėjusio atsparumo kolizijoms šie rezultatai neįrodo. Kolizijos neišvengiamos, nes galimų įvesčių daugiau nei 2^256 išvesčių.

## 6. Lavinos efektas

Kiekvienoje poroje pakeistas vienas ASCII simbolis kitu. Tai gali pakeisti kelis įvesties bitus. Bitams HEX dekoduojamas į baitus, naudojamas XOR ir vienetų skaičiavimas. Bitų skirtumas = skirtingi bitai / 256 × 100; HEX = skirtingos pozicijos / 64 × 100.

| Ilgis | v0.1 bitų vid. % | v0.2 bitų vid. % | v0.1 HEX vid. % | v0.2 HEX vid. % |
|---|---|---|---|---|
| 1 | 24.7459 | 49.9812 | 49.6681 | 93.7335 |
| 2 | 31.2706 | 49.9991 | 61.7119 | 93.7417 |
| 4 | 40.0421 | 50.0113 | 76.6698 | 93.7472 |
| 8 | 45.0510 | 50.0037 | 85.3122 | 93.7754 |
| 10 | 45.9420 | 49.9572 | 86.7964 | 93.7558 |
| 100 | 49.6243 | 50.0066 | 93.1100 | 93.7616 |
| 500 | 49.9137 | 50.0449 | 93.6094 | 93.7958 |
| 1000 | 49.9444 | 49.9564 | 93.6446 | 93.7326 |
| 10000 | 49.9762 | 49.9960 | 93.7264 | 93.7602 |
| all | 42.9456 | 49.9952 | 81.5832 | 93.7560 |


`all` reiškia visus devynis ilgius, 225 000 porų. **Privalomų keturių ilgių** 100 000 porų bitų vidurkis: v0.1 **48.8561 %**, v0.2 **49.9913 %**. Jų HEX vidurkiai: **91.7901 %** ir **93.7615 %**. Šios reikšmės perskaičiuotos iš pradinių porų duomenų, todėl gali paskutiniu skaitmeniu skirtis nuo suapvalintų ilgių vidurkių vidurkio.

<img width="1839" height="625" alt="image" src="https://github.com/user-attachments/assets/88e51b2e-8cd1-487e-a7d7-bd445ad67761" />

Didžiausias pagerėjimas labai trumpoms įvestims: 1 simbolio vidurkis 24,7459 % → 49,9812 %, 2 simbolių 31,2706 % → 49,9991 %. Bendras minimumas 12,1094 % → 35,5469 %. v0.2 pasklidimas šiame rinkinyje nuoseklesnis, tačiau nėra reikalavimo kiekvienai porai pasiekti 50 %. Orientyrai – apie 50 % bitams ir 93,75 % HEX: nepriklausomi bitai sutampa su tikimybe 1/2, HEX pozicija su 1/16.

Histograma rodo porų skaičius 5 procentinių punktų intervaluose; privalomi ilgiai atskirti nuo papildomų. Geri vidurkiai neįrodo saugumo ir nepakeičia minimumų ar pasiskirstymo analizės.

### v0.1: visos lavinos rodiklių reikšmės

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

### v0.2: visos lavinos rodiklių reikšmės

| Ilgis | Poros | Bitų min. % | Bitų maks. % | Bitų vid. % | HEX min. % | HEX maks. % | HEX vid. % |
|---|---|---|---|---|---|---|---|
| 1 | 25000 | 39.4531 | 60.9375 | 49.9812 | 79.6875 | 100.0000 | 93.7335 |
| 2 | 25000 | 39.0625 | 62.5000 | 49.9991 | 78.1250 | 100.0000 | 93.7417 |
| 4 | 25000 | 38.6719 | 61.7188 | 50.0113 | 81.2500 | 100.0000 | 93.7472 |
| 8 | 25000 | 36.3281 | 61.7188 | 50.0037 | 78.1250 | 100.0000 | 93.7754 |
| 10 | 25000 | 35.5469 | 62.8906 | 49.9572 | 79.6875 | 100.0000 | 93.7558 |
| 100 | 25000 | 37.8906 | 63.2812 | 50.0066 | 79.6875 | 100.0000 | 93.7616 |
| 500 | 25000 | 38.6719 | 62.1094 | 50.0449 | 78.1250 | 100.0000 | 93.7958 |
| 1000 | 25000 | 37.1094 | 62.8906 | 49.9564 | 79.6875 | 100.0000 | 93.7326 |
| 10000 | 25000 | 35.5469 | 62.8906 | 49.9960 | 79.6875 | 100.0000 | 93.7602 |
| all | 225000 | 35.5469 | 63.2812 | 49.9952 | 78.1250 | 100.0000 | 93.7560 |



## 7. PIN paieška: be druskos ir su vieša druska

Paieška gauna tik tikslinę maišą ir, kai taikoma, druską. Bandomi visi `0000`–`9999`, nestojant ties sutapimu. Abiem versijoms po 10 matuojamų paleidimų, po 10 000 kandidatų kiekviename: iš viso 100 000 / režimą / versiją. 1000 apšilimo kvietimų prieš režimo matavimus neįtraukti.

| Versija | Režimas | Paleidimai | Kandidatai / kartą | Vid. ms | Min. ms | Maks. ms | Rastas PIN |
|---|---|---|---|---|---|---|---|
| v0.1 | without_salt | 10 | 10000 | 6.113930 | 4.716100 | 14.815500 | 1234 |
| v0.1 | public_salt | 10 | 10000 | 8.121480 | 7.989400 | 8.322000 | 1234 |
| v0.2 | without_salt | 10 | 10000 | 7.375110 | 6.079700 | 15.735700 | 1234 |
| v0.2 | public_salt | 10 | 10000 | 9.252950 | 9.115000 | 9.494400 | 1234 |


<img width="1739" height="448" alt="image" src="https://github.com/user-attachments/assets/cdf3baf3-0479-44a5-91f5-7ad82c659166" />

Laikai yra visos 10 000 kandidatų paieškos, ne vienos maišos. Matuojamas formatavimas, UTF-8 kodavimas, baitų sujungimas, maiša, palyginimas ir sutapimo registravimas. Į konsolę rašoma po matavimo. Excel suvestinėje vidurkis, min. ir maks. skaičiuojami iš visų atskirų laikų.

Druska yra 16 atsitiktinių baitų, išsaugotų HEX faile; į maišą dedami dekoduoti žali baitai: `H(PIN || salt)`. Ji fiksuota visiems vieno taikinio kandidatams ir abiem versijoms vienoda. Vieša druska nesustabdo mažo kandidatų rinkinio perrinkimo: visur rastas `1234`. Šiame seanse v0.2 paieškos vidurkis didesnis abiem režimais; tai dera su papildomo darbo trumpoms įvestims kaina, bet matavimai turi sklaidą, ypač pirmuose be druskos matavimuose.

Skirtingos druskos neleidžia vienos iš anksto apskaičiuotos lentelės naudoti visiems taikiniams. Šis kelių taikinių efektas paaiškinamas konceptualiai, tiesiogiai nematuojamas. Maišos sutapimas apskritai negarantuoja vienintelės pradinės įvesties.

Slaptam `r` konstrukcijoje `H(input || r)` reikėtų ieškoti ir pranešimo, ir nežinomo `r`. Atskleidus juos galima patikrinti maišą. Tai commitment idėja, bet ne šios konstrukcijos slėpimo ar susiejimo saugumo įrodymas. Didelės `r` erdvės paieška neatlikta. Tai nėra iššifravimas ir nėra proof-of-work: PoW ieškoma sąlygą tenkinančios nonce, o čia konkrečios maišos pirmavaizdžio mažame rinkinyje.

## 8. Galutinės išvados

1. Abi versijos praėjo tas pačias teisingumo ir determinizmo patikras.
2. v0.2 aiškiai pagerino tirtų trumpų įvesčių lavinos efektą. Bendras 225 000 porų bitų vidurkis 42,9456 % → 49,9952 %.
3. v0.2 prideda 32 būsenos atnaujinimus ir papildomą sudėtingumą. PIN paieška šiame seanse lėtesnė; didelių įvesčių matavimo pagreitėjimo neinterpretuojame kaip algoritmo garantuoto pagreitėjimo.
4. Abiem versijoms kolizijų nerasta, todėl atsparumo skirtumas nepatvirtintas. Ypač svarbu: **vienodo ilgio v0.1 kolizija išliktų ir v0.2**, nes v0.2 gauna tą pačią būseną ir ilgį. Galutinis maišymas neatkuria prarastos informacijos.
5. Mažas PIN rinkinys perrenkamas ir su vieša druska. Geras lavinos efektas neišplečia kandidatų erdvės.
6. Maišos naudojamos blokams susieti ir Merkle struktūrose. Šiame darbe įgyvendinta funkcija ir jos vertinimas, ne pilna blokų grandinė ir ne saugumo įrodymas.

## DI naudojimas, autorystė ir šaltiniai

Pradinė v0.1 realizacija yra autoriaus esamo projekto algoritmas. v0.2 galutinį maišymą, bendrą bandymų vykdymą, README, analizę ir Excel padėjo parengti Codex / GPT-6. Ankstesnių testų rengimui, aiškinimui ir dešimties PIN paleidimų pakeitimui taip pat naudota ChatGPT / Codex pagalba. Viso testavimo kodo nepriskiriame darbui be DI. Faktinė ankstesnio algoritmo kūrimo istorija vertinama pagal tikrus projekto įrašus.

Svarbios užklausos: įvertinti pradinį projektą, paaiškinti bandymus, sukurti atskirą v0.2 šaką, pagerinti lavinos efektą, PIN bandymą kartoti 10 kartų, palyginti abi versijas vienodomis sąlygomis ir pateikti README bei Excel.

Priimta: išsaugoti v0.1, įtraukti ilgį, pridėti keturis galutinio maišymo ciklus, naudoti tą pačią druską, atskirti privalomus ir papildomus ilgius. Nepasirinkta visiškai perrašyti algoritmo, nes mažesnį pakeitimą lengviau izoliuotai palyginti. Atmesti nepagrįsti saugumo ir garantuoto pagreitėjimo teiginiai. Pasiūlymai tikrinti kompiliavimu, teisingumu, CLI ir visais statistiniais eksperimentais.

Šaltiniai: VU 2026 m. užduotis „Sukurk savo maišos generatorių“ ir kontrolinis sąrašas; esamas projekto kodas; šio seanso `comparison-final` rezultatai; aukščiau nurodytas MurmurHash3 maišymo bloko šaltinis. Konstitucijos failas paimtas iš anksčiau projekte išsaugotos kurso kopijos; tiksli pradinė atsisiuntimo nuoroda neišlikusi. Vietinė v0.1 žyma sukurta 2026-11-05 prie jau buvusio commit, ji neatkuria ankstesnės pateikimo datos ar savarankiškumo įrodymo. Viešą GitHub pateikimą ir atnaujintos analizės commit reikia patikrinti atskirai.
