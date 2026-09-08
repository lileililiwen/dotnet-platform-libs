import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const required = [
  'ui/tokens/tokens.json',
  'ui/packages/design-tokens/dist/tokens.css',
  'ui/packages/design-tokens/dist/tokens.ts',
  'ui/packages/react-ui/src/components.tsx',
  'ui/packages/react-shell/src/shell.tsx',
  'src/Platform.UI.Razor/wwwroot/platform-ui.css',
];

for (const relative of required) assert.ok(existsSync(join(root, relative)), `missing ${relative}`);

const css = readFileSync(join(root, 'ui/packages/design-tokens/dist/tokens.css'), 'utf8');
for (const token of ['--color-background', '--color-foreground', '--color-primary', '--color-danger', '--space-4', '--focus-ring']) {
  assert.match(css, new RegExp(`${token.replaceAll('-', '\\-')}\\s*:`), `missing token ${token}`);
}
assert.match(css, /\.theme-dark|\[data-theme="dark"\]/, 'missing dark theme');
assert.match(css, /prefers-reduced-motion/, 'missing reduced-motion fallback');

const components = readFileSync(join(root, 'ui/packages/react-ui/src/components.tsx'), 'utf8');
const styles = readFileSync(join(root, 'ui/packages/react-ui/styles.css'), 'utf8');
for (const marker of ['aria-busy', 'aria-invalid', 'aria-live']) assert.match(components, new RegExp(marker), `missing accessibility marker ${marker}`);
assert.match(styles, /:focus-visible/, 'missing focus-visible styling');
assert.match(components, /disabled|aria-disabled/, 'missing disabled behavior');
assert.match(components, /role=["']dialog["']|role: ["']dialog["']/, 'missing dialog role');

console.log(`UI verification passed: ${required.length} required artifacts and accessibility markers present.`);
