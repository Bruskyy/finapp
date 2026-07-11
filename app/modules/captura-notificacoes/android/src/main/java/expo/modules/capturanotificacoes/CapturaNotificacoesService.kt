package expo.modules.capturanotificacoes

import android.app.Notification
import android.service.notification.NotificationListenerService
import android.service.notification.StatusBarNotification
import org.json.JSONObject

/**
 * NotificationListenerService do Cofrin (fase 2 do
 * ITEM-CAPTURA-NOTIFICACOES.md). Diferente da lib vendorizada
 * (expo-android-notification-listener-service), a notificação SEMPRE vai pra
 * fila persistente em arquivo - com o app morto nada se perde; o JS drena a
 * fila ao abrir. Se o runtime JS estiver vivo, um evento também é emitido
 * pra fila do AsyncStorage ser atualizada na hora.
 *
 * A allowlist vem de SharedPreferences (persistida por setAllowedPackages no
 * módulo): o filtro funciona mesmo quando o processo renasce só pro serviço,
 * sem o React Native inicializado.
 */
class CapturaNotificacoesService : NotificationListenerService() {
    private var ultimaChave: String? = null
    private var ultimaHora: Long = 0

    override fun onNotificationPosted(sbn: StatusBarNotification) {
        try {
            val prefs = applicationContext.getSharedPreferences(PREFS, MODE_PRIVATE)
            val permitidos = prefs.getStringSet(CHAVE_PACKAGES, emptySet()) ?: emptySet()
            // Allowlist vazia = captura nunca configurada - não coletar nada.
            if (!permitidos.contains(sbn.packageName)) return

            // O Android reposta a mesma notificação em updates rápidos.
            val agora = System.currentTimeMillis()
            if (sbn.key == ultimaChave && agora - ultimaHora < 500) return
            ultimaChave = sbn.key
            ultimaHora = agora

            val extras = sbn.notification.extras
            val json = JSONObject()
                .put("packageName", sbn.packageName)
                .put("title", extras.getCharSequence(Notification.EXTRA_TITLE)?.toString() ?: "")
                .put("text", extras.getCharSequence(Notification.EXTRA_TEXT)?.toString() ?: "")
                .put("bigText", extras.getCharSequence(Notification.EXTRA_BIG_TEXT)?.toString() ?: "")
                .put("postTime", sbn.postTime)
                .put("key", sbn.key)

            FilaNotificacoes.adicionar(applicationContext, json.toString())
            // Aviso "tem coisa nova na fila" pro JS drenar já - sem payload,
            // a fonte de verdade é sempre o arquivo (evita duplicar caminho).
            CapturaNotificacoesModule.avisarFilaAtualizada()
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    companion object {
        const val PREFS = "captura_notificacoes"
        const val CHAVE_PACKAGES = "packages_permitidos"
    }
}
