const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');

const PLUGIN_DIR = path.resolve(__dirname, '../com.xurudragon.xuruvoip.sdPlugin');
const MANIFEST_PATH = path.join(PLUGIN_DIR, 'manifest.json');

test('Stream Deck Plugin Manifest Validation', async (t) => {
    // 1. Ensure manifest.json exists
    await t.test('manifest.json file exists', () => {
        assert.ok(fs.existsSync(MANIFEST_PATH), 'manifest.json does not exist');
    });

    // 2. Load and parse manifest
    let manifest;
    await t.test('manifest.json is valid JSON', () => {
        const content = fs.readFileSync(MANIFEST_PATH, 'utf8');
        try {
            manifest = JSON.parse(content);
        } catch (e) {
            assert.fail('Failed to parse manifest.json: ' + e.message);
        }
        assert.ok(manifest, 'Manifest is empty');
    });

    // 3. Validate root fields
    await t.test('manifest has correct metadata', () => {
        assert.strictEqual(manifest.SDKVersion, 3, 'SDKVersion must be 3');
        assert.strictEqual(manifest.Author, 'XuruDragon', 'Author must be XuruDragon');
        assert.strictEqual(manifest.CodePath, 'bin/plugin.js', 'CodePath must be bin/plugin.js');
        assert.strictEqual(manifest.Category, 'XuruVOIP Control', 'Category must be XuruVOIP Control');
        assert.strictEqual(manifest.CategoryIcon, 'icons/pluginIcon', 'CategoryIcon must be icons/pluginIcon');
        assert.ok(Array.isArray(manifest.Actions), 'Actions must be an array');
        assert.ok(manifest.Actions.length > 0, 'Actions array cannot be empty');
    });

    // 4. Validate actions and referenced assets
    await t.test('all actions have valid properties and referenced assets exist', () => {
        for (const action of manifest.Actions) {
            const uuid = action.UUID;
            assert.ok(uuid, 'Action missing UUID');
            assert.ok(uuid.startsWith('com.xurudragon.xuruvoip.action.'), 'UUID must start with com.xurudragon.xuruvoip.action.');
            assert.ok(action.Name, `Action ${uuid} missing Name`);
            assert.strictEqual(action.PropertyInspectorPath, 'pi/pi.html', `Action ${uuid} must have PropertyInspectorPath set to pi/pi.html`);

            // Verify Action main icon
            const actionIconName = action.Icon;
            assert.ok(actionIconName, `Action ${uuid} missing Icon`);
            const iconPath = path.join(PLUGIN_DIR, actionIconName + '.svg');
            assert.ok(fs.existsSync(iconPath), `Action Icon file does not exist: ${iconPath}`);

            // Verify Property Inspector files
            const piHtmlPath = path.join(PLUGIN_DIR, 'pi/pi.html');
            const piJsPath = path.join(PLUGIN_DIR, 'pi/pi.js');
            assert.ok(fs.existsSync(piHtmlPath), `Property Inspector HTML file does not exist: ${piHtmlPath}`);
            assert.ok(fs.existsSync(piJsPath), `Property Inspector JS file does not exist: ${piJsPath}`);

            // Verify States and their icons
            assert.ok(Array.isArray(action.States), `Action ${uuid} missing States array`);
            assert.ok(action.States.length > 0, `Action ${uuid} has empty States array`);

            for (let i = 0; i < action.States.length; i++) {
                const state = action.States[i];
                const imagePath = path.join(PLUGIN_DIR, state.Image + '.svg');
                assert.ok(fs.existsSync(imagePath), `Action ${uuid} State ${i} image file does not exist: ${imagePath}`);
            }
        }
    });

    // 5. Verify bin/plugin.js exists
    await t.test('compiled background bin/plugin.js file exists', () => {
        const pluginJsPath = path.join(PLUGIN_DIR, 'bin/plugin.js');
        assert.ok(fs.existsSync(pluginJsPath), 'bin/plugin.js background script does not exist');
    });
});
