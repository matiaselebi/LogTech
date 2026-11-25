<?xml version="1.0" encoding="utf-8"?>
<!-- Convierte RemitoDigital.xml (anexo 12.5.3) en una página imprimible del remito. La usa RemitoBLL.Imprimir con
     XslCompiledTransform; también se puede abrir el XML junto a este archivo en un navegador que permita XSLT local. -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" />

  <xsl:template match="/RemitoDigital">
    <html lang="es">
      <head>
        <meta charset="utf-8" />
        <title>Remito digital del viaje N° <xsl:value-of select="IdViaje" /></title>
        <style>
          body { font-family: Arial, sans-serif; color: #10251f; max-width: 720px; margin: 32px auto; padding: 0 16px; }
          h1 { margin: 0 0 4px; font-size: 26px; }
          .sub { color: #4c625b; margin: 0 0 24px; }
          table { width: 100%; border-collapse: collapse; }
          th { text-align: left; width: 34%; color: #4c625b; font-weight: normal; }
          th, td { padding: 9px 6px; border-bottom: 1px solid #d9e2df; vertical-align: top; }
          .token { font-family: Consolas, monospace; font-size: 12px; word-break: break-all; }
          .ok { color: #1a7f37; font-weight: bold; }
          .mal { color: #b42318; font-weight: bold; }
          button { margin-top: 24px; padding: 10px 18px; font-size: 15px; cursor: pointer; }
          @media print { button { display: none; } }
        </style>
      </head>
      <body>
        <h1>Remito digital N° <xsl:value-of select="IdRemito" /></h1>
        <p class="sub">LogTech · Comprobante de entrega del viaje N° <xsl:value-of select="IdViaje" /></p>
        <table>
          <tr><th>Recorrido</th><td><xsl:value-of select="Origen" /> → <xsl:value-of select="Destino" /></td></tr>
          <tr><th>Mercadería</th><td><xsl:value-of select="Mercaderia" /></td></tr>
          <tr><th>Dador de carga</th><td><xsl:value-of select="Dador" /></td></tr>
          <tr><th>Transportista</th><td><xsl:value-of select="Transportista" /></td></tr>
          <tr><th>Recibió</th><td><xsl:value-of select="Receptor" /> (DNI <xsl:value-of select="DniReceptor" />)</td></tr>
          <tr>
            <th>Fecha de entrega</th>
            <!-- FechaEmision viene como 2026-10-15T14:30:00: se muestra 15/10/2026 14:30 -->
            <td>
              <xsl:value-of select="concat(substring(FechaEmision, 9, 2), '/', substring(FechaEmision, 6, 2), '/',
                                           substring(FechaEmision, 1, 4), ' ', substring(FechaEmision, 12, 5))" />
            </td>
          </tr>
          <tr><th>Token SHA-256</th><td class="token"><xsl:value-of select="Token" /></td></tr>
          <tr>
            <th>Verificación</th>
            <td>
              <xsl:choose>
                <xsl:when test="Valido = 'true'"><span class="ok">Verificado: el token coincide con los datos del remito.</span></xsl:when>
                <xsl:otherwise><span class="mal">Alterado: el token no coincide con los datos del remito.</span></xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
        </table>
        <button type="button" onclick="window.print()">Imprimir</button>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
