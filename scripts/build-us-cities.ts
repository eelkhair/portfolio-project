// scripts/build-us-cities.ts
import fs from 'fs';
import path from 'path';

const STATE_TO_CODE: Record<string, string> = {
    Alabama:'AL', Alaska:'AK', Arizona:'AZ', Arkansas:'AR', California:'CA', Colorado:'CO',
    Connecticut:'CT', Delaware:'DE', 'District of Columbia':'DC', Florida:'FL', Georgia:'GA',
    Hawaii:'HI', Idaho:'ID', Illinois:'IL', Indiana:'IN', Iowa:'IA', Kansas:'KS',
    Kentucky:'KY', Louisiana:'LA', Maine:'ME', Maryland:'MD', Massachusetts:'MA', Michigan:'MI',
    Minnesota:'MN', Mississippi:'MS', Missouri:'MO', Montana:'MT', Nebraska:'NE', Nevada:'NV',
    'New Hampshire':'NH', 'New Jersey':'NJ', 'New Mexico':'NM', 'New York':'NY', 'North Carolina':'NC',
    'North Dakota':'ND', Ohio:'OH', Oklahoma:'OK', Oregon:'OR', Pennsylvania:'PA', 'Rhode Island':'RI',
    'South Carolina':'SC', 'South Dakota':'SD', Tennessee:'TN', Texas:'TX', Utah:'UT', Vermont:'VT',
    Virginia:'VA', Washington:'WA', 'West Virginia':'WV', Wisconsin:'WI', Wyoming:'WY',
    'Puerto Rico':'PR', 'Guam':'GU', 'Virgin Islands':'VI'
};

const raw = JSON.parse(fs.readFileSync(path.resolve(__dirname, 'US-States-and-Cities.json'), 'utf8')) as Record<string,string[]>;

// simple cleaner for odd tokens
const clean = (s: string) =>
    s.replace(/\band\b$/i, '').replace(/\s{2,}/g, ' ').trim();

const outSet = new Set<string>();

for (const [stateName, cities] of Object.entries(raw)) {
    const st = STATE_TO_CODE[stateName];
    if (!st) continue;
    for (const c of cities) {
        const city = clean(c);
        if (!city) continue;
        outSet.add(`${city}, ${st}`);
    }
}

const list = Array.from(outSet).sort((a,b) => a.localeCompare(b));
const file = `export const cities: string[] = ${JSON.stringify(list, null, 2)};\n`;

const dest = path.resolve(process.cwd(), 'src/app/data/us-cities.ts');
fs.mkdirSync(path.dirname(dest), { recursive: true });
fs.writeFileSync(dest, file, 'utf8');
console.log(`Wrote ${list.length} entries to ${dest}`);

