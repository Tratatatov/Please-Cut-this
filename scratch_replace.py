import os
import re

directory = r'd:\Projects\Unity\Please Cut this\Assets\_Game\Scripts'

color_map = {
    'GameLoop': 'lightblue',
    'TestGameManager': 'white',
    'ClientView': 'grey',
    'DialogueService': 'white',
    'ClientAnimationService': 'silver',
    'ClientsController': 'cyan',
    'ClientCutsceneViewState': 'yellow',
    'CassetteInsertingViewState': 'orange',
    'DEBUG_STEP': 'magenta',
    'CassetteEjectingViewState': 'orange',
    'GameState': 'cyan'
}

def get_color(prefix):
    return color_map.get(prefix, 'lightblue')

updated_count = 0

for root, dirs, files in os.walk(directory):
    for file in files:
        if file.endswith('.cs'):
            filepath = os.path.join(root, file)
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    content = f.read()
            except Exception as e:
                continue
            
            def repl_all(m):
                prefix = m.group(1)
                # Ignore already colored or specific terms that aren't log prefixes
                if prefix in ['Space', 'SerializeReference']:
                    return m.group(0)
                color = get_color(prefix)
                return f'<color={color}>[{prefix}]</color>'
                
            # Regex to match [Prefix] only if it is likely at the start of a log string like "[Prefix]" or $"[Prefix]"
            # It matches a quote (or $" ), optional spaces, then [Prefix]
            new_content = re.sub(r'(["' + r"']\s*|^\s*\$?\s*[\"']\s*|\s+)\[([A-Za-z0-9_]+)\]", lambda m: m.group(1) + repl_all(m), content)
            
            # To be safer, just match `"[Prefix]` and `$"[Prefix]`
            def safe_repl(m):
                prefix = m.group(2)
                if prefix in ['Space', 'SerializeReference', 'SerializeField', 'Header']:
                    return m.group(0)
                color = get_color(prefix)
                return f'{m.group(1)}<color={color}>[{prefix}]</color>'
                
            new_content = re.sub(r'(\$\"|\")\[([A-Za-z0-9_]+)\]', safe_repl, content)
            
            if new_content != content:
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                updated_count += 1
                print(f'Updated {filepath}')

print(f'Total files updated: {updated_count}')
