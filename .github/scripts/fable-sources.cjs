const fs = require('node:fs');
const path = require('node:path');
const { zipEntries } = require('./packages.cjs');

const expectedLayouts = [
  {
    prefix: 'Ranvier.Keyboard.',
    excludePrefix: 'Ranvier.Keyboard.Dom.',
    files: [
      'fable/Ranvier.Keyboard.fsproj',
      'fable/IsoAndW3cKeys.fs',
      'fable/UnicodeAndControl.fs',
      'fable/DeadKeysAndDigraphs.fs',
      'fable/KeyChord.fs',
    ],
  },
  {
    prefix: 'Fable.Ranvier.Keyboard.Dom.',
    excludePrefix: null,
    files: [
      'fable/Fable.Ranvier.Keyboard.Dom.fsproj',
      'fable/KeyboardEvent.fs',
    ],
  },
];

const packagesDirectory = process.argv[2] || 'bin';
const files = fs.readdirSync(packagesDirectory).filter(f => f.endsWith('.nupkg'));
const problems = [];

for (const file of files) {
  for (const spec of expectedLayouts) {
    if (!file.startsWith(spec.prefix)) continue;
    if (spec.excludePrefix && file.startsWith(spec.excludePrefix)) continue;
    const entries = zipEntries(fs.readFileSync(path.join(packagesDirectory, file)));
    for (const expected of spec.files) {
      if (!entries.has(expected)) {
        problems.push(`${file}: missing ${expected}`);
      }
    }
  }
}

for (const problem of problems) console.error(problem);
if (problems.length > 0) process.exit(1);
console.log(`Fable source layouts verified in ${packagesDirectory}.`);
