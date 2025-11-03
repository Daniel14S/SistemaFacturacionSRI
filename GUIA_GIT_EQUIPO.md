# 🌿 Guía de Git para el Equipo - Sistema Facturación SRI

## 📋 Configuración Inicial (Solo la primera vez)

### 1. Clonar el repositorio (si aún no lo tienen)
```bash
git clone https://github.com/REEMPLAZAR_CON_TU_USUARIO/SistemaFacturacionSRI.git
cd SistemaFacturacionSRI
```

### 2. Si YA tienen el repositorio clonado, actualizar:
```bash
git fetch origin
git checkout develop
git pull origin develop
```

### 3. Configurar tu información (si no lo has hecho):
```bash
git config --global user.name "Tu Nombre Completo"
git config --global user.email "tuemail@ejemplo.com"
```

---

## 🚀 Crear tu rama personal (SOLO LA PRIMERA VEZ)

Cada uno debe crear su rama según su trabajo:

**Patricio:**
```bash
git checkout develop
git checkout -b feature/patricio-database
git push -u origin feature/patricio-database
```

**Kerly:**
```bash
git checkout develop
git checkout -b feature/kerly-controllers
git push -u origin feature/kerly-controllers
```

**Melany:**
```bash
git checkout develop
git checkout -b feature/melany-frontend
git push -u origin feature/melany-frontend
```

**Pedro:**
```bash
git checkout develop
git checkout -b feature/pedro-backend
git push -u origin feature/pedro-backend
```

---

## 🔄 Flujo de Trabajo Diario

### AL INICIO DEL DÍA (ANTES DE TRABAJAR):
```bash
# 1. Ir a develop y actualizarla
git checkout develop
git pull origin develop

# 2. Ir a tu rama personal
git checkout feature/TU-NOMBRE-MODULO

# 3. Traer cambios de develop a tu rama
git merge develop
```

**¿Por qué hacer esto?**
- Para tener los últimos cambios que hicieron tus compañeros
- Evita conflictos grandes al final

---

### MIENTRAS TRABAJAS:

**Guardar cambios cada 1-2 horas:**
```bash
# Ver qué archivos cambiaste
git status

# Agregar todos los cambios
git add .

# Hacer commit con mensaje descriptivo
git commit -m "T-XX: Descripción de lo que hiciste"

# Subir a GitHub (backup en la nube)
git push origin feature/TU-NOMBRE-MODULO
```

**Ejemplos de buenos mensajes de commit:**
- ✅ `git commit -m "T-15: Configuración de Fluent API para Producto"`
- ✅ `git commit -m "T-25: Implementado endpoint GET /api/productos"`
- ❌ `git commit -m "cambios"` (muy vago)
- ❌ `git commit -m "fix"` (no dice qué arregló)

---

### AL TERMINAR UNA TAREA:

**1. Hacer último commit y push:**
```bash
git add .
git commit -m "T-XX: Completada [descripción detallada]"
git push origin feature/TU-NOMBRE-MODULO
```

**2. Crear Pull Request en GitHub:**

a) Ve a: https://github.com/USUARIO/SistemaFacturacionSRI

b) Verás un banner amarillo que dice:
   **"feature/TU-NOMBRE had recent pushes"**
   → Click en **"Compare & pull request"**

c) Verificar que diga:
   - **base:** `develop` ← 
   - **compare:** `feature/TU-NOMBRE` →

d) Llenar información:
   - **Título:** Nombre de la tarea (ej: "T-19: Backend ProductoService Crear")
   - **Descripción:** Explica qué hiciste, qué probaste

e) Click en **"Create pull request"**

f) **Asignar a Pedro** para revisión (en la columna derecha)

**3. Notificar en WhatsApp:**
```
✅ Terminé T-XX: [descripción]
📝 Pull Request creado, por favor revisar @Pedro
```

---

## 🆘 Comandos Útiles

### Ver en qué rama estás:
```bash
git branch
# La que tiene * es donde estás
```

### Ver todas las ramas (locales y remotas):
```bash
git branch -a
```

### Cambiar de rama:
```bash
git checkout nombre-de-la-rama
```

### Ver el historial de commits:
```bash
git log --oneline
```

### Deshacer cambios NO guardados (¡CUIDADO! se pierden):
```bash
git checkout -- .
```

### Ver diferencias de lo que cambiaste:
```bash
git diff
```

---

## 🚨 Solución de Problemas Comunes

### Problema: "Your branch is behind 'origin/develop'"
**Solución:**
```bash
git pull origin develop
```

### Problema: "Please commit your changes or stash them"
**Solución:** Debes guardar tus cambios primero:
```bash
git add .
git commit -m "WIP: Trabajo en progreso"
```

### Problema: Conflictos al hacer merge
**Solución:**
1. VS Code te mostrará los conflictos
2. Elige qué cambios conservar
3. Guarda los archivos
4. Haz commit:
```bash
git add .
git commit -m "Resuelto conflicto con develop"
```

### Problema: Olvidé en qué rama estaba trabajando
**Solución:**
```bash
git status
# Te dice la rama actual
```

---

## 📊 Estructura de Ramas
```
main (producción - PROTEGIDA)
  |
  └── develop (desarrollo en equipo)
       |
       ├── feature/pedro-backend
       ├── feature/patricio-database
       ├── feature/kerly-controllers
       └── feature/melany-frontend
```

---

## ✅ Reglas del Equipo

**SÍ HACER:**
- ✅ Trabajar en tu rama personal
- ✅ Commits frecuentes (cada 1-2 horas)
- ✅ Mensajes descriptivos
- ✅ Pull Request al terminar tarea
- ✅ Actualizar desde develop antes de trabajar
- ✅ Hacer push al final del día (backup)

**NO HACER:**
- ❌ Push directo a `main` (está protegida)
- ❌ Push directo a `develop` (solo por PR)
- ❌ Trabajar en la rama de otro compañero
- ❌ Commits con mensajes vagos
- ❌ Olvidar hacer pull al inicio del día

---

## 📞 Ayuda

- Pregunta en WhatsApp del grupo
- Pide ayuda en el Daily Scrum
- Contacta a Pedro (Scrum Master)

---

## 🎯 Resumen del Día a Día
```
MAÑANA:
1. git checkout develop
2. git pull origin develop
3. git checkout feature/mi-rama
4. git merge develop
5. [TRABAJAR]

DURANTE:
6. git add .
7. git commit -m "T-XX: descripción"
8. git push origin feature/mi-rama

NOCHE:
9. Último commit + push
10. Si terminé tarea → Pull Request
```