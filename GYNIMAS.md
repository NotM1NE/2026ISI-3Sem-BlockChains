# Pasiruošimas gynimui

## 60 sekundžių pristatymas

Sukūriau mokomąją 256 bitų maišos funkciją. Ji ima baitus ir grąžina 64 HEX simbolius. Būsenoje yra aštuoni 32 bitų skaičiai. Kiekvienas įvesties baitas pakeičia vieną būsenos elementą, o tada jo įtaka perduodama per likusius elementus. Naudoju sudėtį, daugybą ir bitų rotacijas su 32 bitų persipildymu.

Pradinė versija trumpoms įvestims turėjo silpnesnį lavinos efektą: pasikeisdavo vidutiniškai 46,07 % bitų. Su DI pagalba v0.2 pridėjome įvesties ilgį ir keturis galutinio maišymo ciklus. Tais pačiais bandymais trumpoms įvestims gavome 49,98 %, bendras vidurkis 49,99 %. Tai pagerino pakeitimo pasklidimą, tačiau neįrodo kriptografinio saugumo.

## Kaip perskaityti kodą

1. `Program.cs`: UTF-8, pasirinktinė `--version`, eksperimento arba teksto / failo režimo pasirinkimas.
2. `HashInitialState.Create()`: kiekvienam kvietimui NAUJAS masyvas `[10,20,...,80]`, todėl ankstesnė įvestis nepalieka būsenos.
3. `ComputeV01`: baitas pridedamas, dauginama iš 7, rotuojama 5 bitais; per likusius žodžius perduodama įtaka, dauginant ankstesnį iš 3 ir rotuojant 11 bitų.
4. `Compute`: v0.1 grąžinama nepakeista arba vyksta v0.2 galutinis maišymas. Ilgio apatiniai ir viršutiniai 32 bitai įtraukiami XOR. Keturi ciklai po 8 atnaujinimus = 32 atnaujinimai.
5. `X8`: kiekvieną `uint` išveda aštuoniais HEX simboliais, išsaugodamas nulius. 8 × 8 = 64 HEX, 64 × 4 = 256 bitų.

## Klausimai, kuriuos turi mokėti paaiškinti

**Kas yra XOR?** Bitas yra 1, jei du lyginami bitai skiriasi. `5 XOR 3`: `101 XOR 011 = 110`, t. y. 6.

**Rotacija ir poslinkis?** Poslinkis išstumia bitus, rotacija juos grąžina kitame gale. `uint` yra 32 bitų, tad `RotateLeft` suka 32 bitų žodį.

**Kam `unchecked`?** Sudėties ir daugybos rezultatai apvyniojami modulo 2^32. Tai sąmoninga algoritmo dalis, o ne klaida.

**Kodėl fiksuotas ilgis?** Bet kiek įvesties baitų visada apdorojama toje pačioje aštuonių žodžių būsenoje. Ilgis nepriklauso nuo teksto ilgio.

**Kodėl vienodi tekstai kartais duoda kitą maišą?** Reikšmingi tikslūs baitai: tarpai, raidžių registras, BOM, LF / CRLF, Unicode koduotė. `ReadLine` neįtraukia Enter pabaigos, failas apdorojamas visas. Lietuviškas simbolis gali užimti kelis UTF-8 baitus.

**Kas yra determinizmas?** Tie patys baitai duoda tą patį rezultatą. Tikrinau kartojimą, A–B–A ir atskirus paleidimus. Atsitiktinumas naudojamas bandymų duomenims, ne pačiai maišos funkcijai.

**Kas yra kolizija?** Dvi skirtingos įvestys su ta pačia maiša. Jos neišvengiamos, nes įvesčių daugiau nei 2^256 galimų išvesčių. Neradimas ribotame rinkinyje neįrodo, kad jų sunku rasti.

**Kas yra pirmavaizdis?** Turint konkrečią maišą, ieškoti ją duodančios įvesties. Kolizijų paieškoje tinka bet kuri sutampanti pora, nereikia iš anksto nustatytos maišos.

**Kodėl 50 % ir 93,75 %?** Nepriklausomi bitai skiriasi su tikimybe 1/2. HEX simbolis iš 16 reikšmių sutampa su tikimybe 1/16, taigi skiriasi 15/16 = 93,75 %. Tai vidurkiai, ne ribos kiekvienai porai.

**Kaip skaičiuojamas lavinos efektas?** Dekoduoju maišų HEX į baitus, XOR parodo skirtingus bitus, suskaičiuoju vienetus ir dalinu iš 256. HEX skirtumui skaičiuoju skirtingas pozicijas iš 64. Vienas pakeistas simbolis gali pakeisti kelis įvesties bitus.

**Ką ištaisė v0.2?** Po paskutinio įvesties baito dar sumaišoma būsena. Taip mažinama silpnų trumpų įvesčių uodega. Tačiau vienodo ilgio v0.1 kolizija lieka v0.2 kolizija: vien galutinio maišymo neužtenka ją pašalinti.

**Ką pablogino v0.2?** Pridėjo 32 žodžių atnaujinimus kiekvienai maišai, daugiau kodo ir operacijų. Įtaka santykinai didesnė mažoms įvestims. Vieno seanso spartos svyravimų nepateikiu kaip pagreitėjimo įrodymo.

**Kodėl druska neapsaugo 4 skaitmenų PIN?** Yra tik 10 000 kandidatų, viešą druską gali pridėti ir užpuolikas. Skirtingos druskos trukdo tą pačią iš anksto sudarytą lentelę naudoti skirtingiems taikiniams.

**O slaptas r?** Paieškoje reikėtų atspėti ir pranešimą, ir r. Atskleidus abu perskaičiuojama maiša. Tai commitment idėja, bet šios konstrukcijos saugumas neįrodytas.

**Ar tai proof-of-work?** Ne. Čia ieškau konkrečios maišos pirmavaizdžio mažame kandidatų rinkinyje. PoW ieškoma nonce, kad maiša atitiktų sąlygą, pvz., turėtų pakankamai nulinių pradžios bitų.

**Ryšys su blokų grandinėmis?** Blokas gali saugoti ankstesnio bloko maišą, todėl ankstesnių duomenų pakeitimas pakeičia nuorodą. Ši užduotis kuria maišos funkciją ir ją tiria; pilnos blokų grandinės čia nekūriau.

## 30 minučių planas

- 10 min.: atsidaryk `ComputeV01` ir savais žodžiais pereik per vieno baito apdorojimą.
- 10 min.: atsidaryk `Compute`, palyginimo lentelę ir histogramą. Paaiškink kodėl pagerėjimas nėra saugumo įrodymas.
- 5 min.: paleisk abi versijas su tuo pačiu trumpu tekstu ir parodyk 64 HEX išvestį.
- 5 min.: garsiai atsakyk apie koliziją, pirmavaizdį, druską ir 50 % orientyrą.

Sąžiningai atskirk savo pradinę realizaciją nuo DI pasiūlyto v0.2 galutinio maišymo ir testų pagalbos. Nepriskirk sau darbo be DI, jei jo istorija kitokia.
