(function () {
    "use strict";
    function tiene(objeto, clave) {
        return Object.prototype.hasOwnProperty.call(objeto, clave);
    }

    async function traducirPagina() {
        var idioma = document.documentElement.lang;
        if (!idioma || idioma === "es" || idioma === "es-ES") return;
        var url = document.documentElement.dataset.traduccionesUrl;
        var traducciones = window.traduccionesGuardadas || {};

        // Claves con {0}, {1}...: plantillas para textos que llevan datos ("Viaje N° {0}").
        // Primero las más largas, para que "Página {0} de {1}" gane sobre "{0} de 5".
        var plantillas = Object.keys(traducciones).filter(function (clave) {
            return clave.indexOf("{0}") >= 0;
        }).map(function (clave) {
            var patron = clave.replace(/[.*+?^${}()|[\]\\]/g, "\\$&").replace(/\\\{\d\\\}/g, "(.+?)");
            return { clave: clave, regex: new RegExp("^" + patron + "$"), largo: clave.replace(/\{\d\}/g, "").length };
        }).sort(function (a, b) { return b.largo - a.largo; });

        // Devuelve la traducción del texto, o null si no hay.
        function traducir(texto) {
            if (tiene(traducciones, texto)) return traducciones[texto];
            for (var i = 0; i < plantillas.length; i++) {
                var partes = plantillas[i].regex.exec(texto);
                if (!partes) continue;
                // Cada dato se traduce si es una palabra conocida (por ejemplo, un estado del viaje).
                return traducciones[plantillas[i].clave].replace(/\{(\d)\}/g, function (marca, numero) {
                    var dato = partes[Number(numero) + 1];
                    return tiene(traducciones, dato) ? traducciones[dato] : dato;
                });
            }
            return null;
        }

        // Los mensajes de confirmación (confirm) también se muestran traducidos.
        var confirmar = window.confirm;
        window.confirm = function (mensaje) {
            return confirmar.call(window, traducir(mensaje) || mensaje);
        };

        var elementos = [];
        function excluido(elemento) {
            return !elemento || elemento.closest("script,style,textarea,code,pre,.no-translate,[translate='no']");
        }
        function agregar(valor, aplicar) {
            var texto = valor.trim();
            if (texto && texto.length <= 500 && /[a-zA-ZÀ-ÿ]/.test(texto))
                elementos.push({ texto: texto, aplicar: aplicar });
        }
        var recorrido = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        var nodo;
        while ((nodo = recorrido.nextNode())) {
            if (excluido(nodo.parentElement)) continue;
            (function (actual) {
                var original = actual.nodeValue;
                agregar(original, function (valor) {
                    actual.nodeValue = original.replace(original.trim(), valor);
                });
            }(nodo));
        }
        document.querySelectorAll("[placeholder],[title],[aria-label],input[type=submit],input[type=button],input[type=reset]").forEach(function (elemento) {
            if (excluido(elemento)) return;
            ["placeholder", "title", "aria-label"].forEach(function (atributo) {
                var valor = elemento.getAttribute(atributo);
                if (valor) agregar(valor, function (traduccion) { elemento.setAttribute(atributo, traduccion); });
            });
            if (elemento.matches("input[type=submit],input[type=button],input[type=reset]"))
                agregar(elemento.value, function (valor) { elemento.value = valor; });
        });
        agregar(document.title, function (valor) { document.title = valor; });

        // Se aplica como texto, nunca como HTML.
        function aplicarTraducciones() {
            elementos.forEach(function (x) {
                var traduccion = traducir(x.texto);
                if (traduccion !== null) x.aplicar(traduccion);
            });
        }
        aplicarTraducciones();

        // Lo que no está guardado se le pide al servidor (que solo traduce textos de la interfaz).
        var faltan = Array.from(new Set(elementos.map(function (x) { return x.texto; }))).filter(function (texto) {
            return traducir(texto) === null;
        });
        if (!faltan.length || !url) return;
        try {
            for (var inicio = 0; inicio < faltan.length; inicio += 250) {
                var lote = faltan.slice(inicio, inicio + 250);
                var anteriores = -1;
                // El servidor consulta pocos textos por pedido: se repite mientras traiga novedades.
                while (true) {
                    var respuesta = await fetch(url, {
                        method: "POST", headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({ textos: lote })
                    });
                    if (!respuesta.ok || !(respuesta.headers.get("Content-Type") || "").includes("application/json")) return;
                    var datos = (await respuesta.json()).d || {};
                    Object.assign(traducciones, datos);
                    aplicarTraducciones();
                    var cantidad = Object.keys(datos).length;
                    if (cantidad === anteriores || cantidad === lote.length) break;
                    anteriores = cantidad;
                }
            }
        } catch (error) {
            // Sin conexión con el servicio la página queda en español: no es un error para el usuario.
            console.warn("LogTech: no se pudieron pedir las traducciones que faltan.", error);
        }
    }
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", traducirPagina);
    else traducirPagina();
}());
