// Validación de los formularios (R48): los campos no dejan escribir caracteres que no van (un DNI solo acepta
// números y hasta 8, una patente se pasa a mayúsculas...). El tipo de cada campo va en el atributo data-tipo y el
// largo en maxlength. Los mensajes de error los muestran los controles de validación de ASP.NET
// (RequiredFieldValidator, RegularExpressionValidator...), y la capa de negocio vuelve a controlar todo en el
// servidor (Validador.cs).
(function () {
    "use strict";

    // Qué caracteres se borran según el tipo de campo.
    var filtros = {
        digitos: function (v) { return v.replace(/\D/g, ""); },
        nombre: function (v) { return v.replace(/[^A-Za-zÀ-ÖØ-öø-ÿ' \-]/g, ""); },
        correo: function (v) { return v.replace(/\s/g, ""); },
        usuario: function (v) { return v.toLowerCase().replace(/[^a-z0-9._\-]/g, ""); },
        licencia: function (v) { return v.replace(/[^A-Za-z0-9\-]/g, ""); },
        patente: function (v) { return v.toUpperCase().replace(/[^A-Z0-9]/g, ""); },
        cbu: function (v) { return v.replace(/[^A-Za-z0-9.\-]/g, ""); }
    };

    // La tarjeta además se escribe con su formato: «4509 9535 6623 3704» (16 números).
    var formatos = {
        tarjeta: function (v) { return v.replace(/\D/g, "").slice(0, 16).replace(/(\d{4})(?=\d)/g, "$1 "); }
    };

    document.addEventListener("input", function (e) {
        var campo = e.target, tipo = campo.getAttribute && campo.getAttribute("data-tipo");
        if (filtros[tipo]) {
            var limpio = filtros[tipo](campo.value);
            if (limpio === campo.value) return;
            var posicion = campo.selectionStart;   // null en los campos de correo (type=email)
            var cursor = posicion === null ? null : filtros[tipo](campo.value.slice(0, posicion)).length;
            campo.value = limpio;
            if (cursor !== null) campo.setSelectionRange(cursor, cursor);
        } else if (formatos[tipo]) {
            var nuevo = formatos[tipo](campo.value);
            if (nuevo !== campo.value) campo.value = nuevo;
        }
    });

    // Para los CustomValidator de montos, pesos y fechas: el valor tiene que estar entre los atributos min y max del
    // campo. Las fechas (aaaa-mm-dd) y los meses (aaaa-mm) se comparan como texto.
    window.validarRango = function (validador, args) {
        var campo = document.getElementById(validador.controltovalidate);
        var valor = args.Value, min = campo.min, max = campo.max;
        if (campo.type !== "date" && campo.type !== "month") {
            valor = parseFloat(valor); min = parseFloat(min); max = parseFloat(max);
            if (isNaN(valor)) { args.IsValid = false; return; }
        }
        args.IsValid = !(valor < min) && !(valor > max);
    };

    // Número de tarjeta: 16 números de Visa o Mastercard con el dígito verificador (Luhn) bien. El servidor lo vuelve
    // a controlar en PasarelaPagoServicio.
    window.validarTarjeta = function (validador, args) {
        var numero = args.Value.replace(/\D/g, ""), suma = 0, i, digito;
        if (!/^(4\d{15}|5[1-5]\d{14}|2(2[2-9]|[3-6]\d|7[01])\d{13}|2720\d{12})$/.test(numero)) { args.IsValid = false; return; }
        for (i = 0; i < 16; i++) {
            digito = parseInt(numero.charAt(15 - i), 10);
            if (i % 2 === 1) { digito *= 2; if (digito > 9) digito -= 9; }
            suma += digito;
        }
        args.IsValid = suma % 10 === 0;
    };
}());
