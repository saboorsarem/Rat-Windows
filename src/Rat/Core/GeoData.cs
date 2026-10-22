namespace Rat;

public sealed record CountryInfo(string Name, double Lat, double Lon);

public static class GeoData
{
    static readonly Dictionary<string, CountryInfo> Countries = new(StringComparer.OrdinalIgnoreCase);

    static GeoData()
    {
        foreach (var line in Table.Split('\n'))
        {
            var parts = line.Trim().Split('|');
            if (parts.Length != 4) continue;
            if (!double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)) continue;
            if (!double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon)) continue;
            Countries[parts[0]] = new CountryInfo(parts[1], lat, lon);
        }
    }

    public static bool TryGet(string code, out CountryInfo info) => Countries.TryGetValue(code, out info!);

    public static string NameOf(string code) => Countries.TryGetValue(code, out var c) ? c.Name : code;

    const string Table = """
US|United States|39.8|-98.6
CA|Canada|56.1|-106.3
MX|Mexico|23.6|-102.5
BR|Brazil|-14.2|-51.9
AR|Argentina|-38.4|-63.6
CL|Chile|-35.7|-71.5
CO|Colombia|4.6|-74.3
PE|Peru|-9.2|-75.0
VE|Venezuela|6.4|-66.6
EC|Ecuador|-1.8|-78.2
BO|Bolivia|-16.3|-63.6
PY|Paraguay|-23.4|-58.4
UY|Uruguay|-32.5|-55.8
CU|Cuba|21.5|-77.8
DO|Dominican Republic|18.7|-70.2
GT|Guatemala|15.8|-90.2
CR|Costa Rica|9.7|-83.8
PA|Panama|8.5|-80.8
PR|Puerto Rico|18.2|-66.4
JM|Jamaica|18.1|-77.3
HT|Haiti|18.9|-72.3
HN|Honduras|14.8|-86.2
SV|El Salvador|13.8|-88.9
NI|Nicaragua|12.9|-85.2
TT|Trinidad and Tobago|10.7|-61.3
BB|Barbados|13.2|-59.5
BS|Bahamas|24.7|-78.0
GB|United Kingdom|54.0|-2.9
IE|Ireland|53.1|-8.2
FR|France|46.6|1.9
DE|Germany|51.2|10.4
NL|Netherlands|52.1|5.3
BE|Belgium|50.5|4.5
LU|Luxembourg|49.8|6.1
CH|Switzerland|46.8|8.2
AT|Austria|47.5|14.6
IT|Italy|42.8|12.1
ES|Spain|40.2|-3.7
PT|Portugal|39.4|-8.2
SE|Sweden|62.0|15.0
NO|Norway|64.5|12.0
FI|Finland|64.0|26.0
DK|Denmark|56.0|10.0
IS|Iceland|64.9|-18.6
PL|Poland|52.1|19.4
CZ|Czechia|49.8|15.5
SK|Slovakia|48.7|19.5
HU|Hungary|47.2|19.5
RO|Romania|45.9|25.0
BG|Bulgaria|42.7|25.5
GR|Greece|39.0|22.0
HR|Croatia|45.1|15.2
SI|Slovenia|46.1|14.8
RS|Serbia|44.0|21.0
BA|Bosnia and Herzegovina|43.9|17.7
ME|Montenegro|42.7|19.3
MK|North Macedonia|41.6|21.7
AL|Albania|41.1|20.1
LT|Lithuania|55.2|23.9
LV|Latvia|56.9|24.9
EE|Estonia|58.7|25.5
BY|Belarus|53.7|27.9
UA|Ukraine|49.0|31.4
MD|Moldova|47.4|28.4
RU|Russia|61.5|100.0
TR|Turkey|39.0|35.2
CY|Cyprus|35.1|33.2
MT|Malta|35.9|14.4
GI|Gibraltar|36.1|-5.3
AD|Andorra|42.5|1.5
MC|Monaco|43.7|7.4
GE|Georgia|42.3|43.4
AM|Armenia|40.1|45.0
AZ|Azerbaijan|40.3|47.5
KZ|Kazakhstan|48.0|66.9
UZ|Uzbekistan|41.4|64.6
KG|Kyrgyzstan|41.2|74.8
TJ|Tajikistan|38.9|71.3
TM|Turkmenistan|38.9|59.6
AF|Afghanistan|33.9|67.7
PK|Pakistan|30.4|69.3
IN|India|22.4|79.0
BD|Bangladesh|23.7|90.3
LK|Sri Lanka|7.6|80.7
NP|Nepal|28.4|84.1
BT|Bhutan|27.5|90.4
CN|China|35.9|104.2
JP|Japan|36.2|138.3
KR|South Korea|36.5|127.9
KP|North Korea|40.1|127.2
TW|Taiwan|23.6|121.0
HK|Hong Kong|22.4|114.1
SG|Singapore|1.35|103.82
MY|Malaysia|4.1|102.0
ID|Indonesia|-2.5|118.0
TH|Thailand|15.1|101.0
VN|Vietnam|16.2|107.8
PH|Philippines|12.9|121.8
MM|Myanmar|21.9|96.0
KH|Cambodia|12.6|104.9
LA|Laos|19.9|102.5
BN|Brunei|4.5|114.7
AU|Australia|-25.3|133.8
NZ|New Zealand|-40.9|174.9
FJ|Fiji|-17.8|178.1
PG|Papua New Guinea|-6.3|143.9
MN|Mongolia|46.9|103.8
SA|Saudi Arabia|23.9|45.1
AE|United Arab Emirates|24.0|54.0
QA|Qatar|25.3|51.2
KW|Kuwait|29.3|47.5
BH|Bahrain|26.0|50.6
OM|Oman|21.0|57.0
YE|Yemen|15.6|48.0
IQ|Iraq|33.2|43.7
IR|Iran|32.4|53.7
IL|Israel|31.0|34.9
JO|Jordan|31.3|36.4
LB|Lebanon|33.9|35.9
SY|Syria|34.8|39.0
PS|Palestine|31.9|35.2
EG|Egypt|26.8|30.8
LY|Libya|26.3|17.2
TN|Tunisia|33.9|9.5
DZ|Algeria|28.0|1.7
MA|Morocco|31.8|-7.1
SD|Sudan|15.6|30.2
SS|South Sudan|7.3|30.3
ET|Ethiopia|9.1|40.5
ER|Eritrea|15.2|39.8
DJ|Djibouti|11.8|42.6
SO|Somalia|5.2|46.2
KE|Kenya|0.2|37.9
TZ|Tanzania|-6.4|34.9
UG|Uganda|1.4|32.3
RW|Rwanda|-2.0|29.9
BI|Burundi|-3.4|29.9
NG|Nigeria|9.1|8.7
GH|Ghana|7.9|-1.0
CI|Ivory Coast|7.5|-5.5
SN|Senegal|14.5|-14.5
CM|Cameroon|7.4|12.4
AO|Angola|-11.2|17.9
ZM|Zambia|-13.1|27.8
ZW|Zimbabwe|-19.0|29.2
MZ|Mozambique|-18.7|35.5
ZA|South Africa|-30.6|22.9
NA|Namibia|-22.9|18.5
BW|Botswana|-22.3|24.7
MG|Madagascar|-18.8|46.9
MU|Mauritius|-20.3|57.6
MW|Malawi|-13.3|34.3
CD|DR Congo|-2.9|23.7
CG|Congo|-0.2|15.8
GA|Gabon|-0.8|11.6
TD|Chad|15.5|18.7
NE|Niger|17.6|8.1
ML|Mali|17.6|-4.0
BF|Burkina Faso|12.2|-1.6
GN|Guinea|9.9|-9.7
SL|Sierra Leone|8.5|-11.8
LR|Liberia|6.4|-9.4
TG|Togo|8.6|0.8
BJ|Benin|9.3|2.3
GM|Gambia|13.4|-15.3
GW|Guinea-Bissau|11.8|-15.2
MR|Mauritania|21.0|-10.9
CF|Central African Republic|6.6|20.9
GQ|Equatorial Guinea|1.7|10.3
""";
}
