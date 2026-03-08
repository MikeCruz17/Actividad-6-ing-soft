# Proyecto STR – Ingeniería de Software en Tiempo Real (C#)

## Autor
- **Nombre:** Miguel Ángel Cruz Fernández  
- **Matrícula:** 24-0195  
- **Asignatura:** TI3521 – Ingeniería de Software en Tiempo Real  
- **Actividad:** Implementación de Sistemas de Tiempo Real (STR)  
- **Lenguaje:** C# (.NET Console)

---

## Descripción General

Este proyecto implementa un **Sistema de Tiempo Real (STR)** en **C#**, desarrollado a lo largo de la asignatura, y compuesto por **dos módulos** que comparten la misma filosofía y arquitectura de tiempo real:

1. **STR de Alerta Temprana por Inundaciones** (proyecto base del curso)  
2. **STR de Semáforo** (actividad práctica solicitada)

Ambos sistemas utilizan:
- Tareas periódicas
- Deadlines
- Máquinas de estados
- Registro en consola con sellos de tiempo

El proyecto demuestra cómo los conceptos de **tiempo real** pueden reutilizarse y adaptarse a distintos problemas.

---

## Arquitectura General del Proyecto

El sistema se ejecuta desde una **aplicación de consola** con un menú principal que permite seleccionar qué STR ejecutar:

```text
[1] STR Inundaciones (Simulación)
[2] STR Semáforo (Actividad)
