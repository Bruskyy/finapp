package expo.modules.capturanotificacoes

import android.content.Context
import android.content.Intent
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import expo.modules.kotlin.modules.Module
import expo.modules.kotlin.modules.ModuleDefinition
import java.util.concurrent.atomic.AtomicBoolean

class CapturaNotificacoesModule : Module() {
    override fun definition() = ModuleDefinition {
        Name("CapturaNotificacoes")

        // Evento sem payload: só avisa que a fila persistente ganhou item
        // novo enquanto o app está aberto - o JS responde drenando. A fonte
        // de verdade é sempre o arquivo (FilaNotificacoes), nunca o evento.
        Events("onFilaAtualizada")

        OnCreate { pronto.set(true) }
        OnDestroy {
            pronto.set(false)
            instancia = null
        }

        Function("isNotificationPermissionGranted") {
            val context = appContext.reactContext ?: return@Function false
            val habilitados = Settings.Secure.getString(context.contentResolver, "enabled_notification_listeners")
            habilitados?.contains(context.packageName) ?: false
        }

        Function("openNotificationListenerSettings") {
            val context = appContext.reactContext ?: return@Function Unit
            val intent = Intent(Settings.ACTION_NOTIFICATION_LISTENER_SETTINGS)
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            context.startActivity(intent)
            Unit
        }

        // Persistida em SharedPreferences (não em memória, como na lib
        // original): o serviço filtra pela allowlist mesmo quando o processo
        // renasce só pra ele, sem o React Native inicializado.
        Function("setAllowedPackages") { packages: List<String> ->
            val context = appContext.reactContext ?: return@Function Unit
            context.getSharedPreferences(CapturaNotificacoesService.PREFS, Context.MODE_PRIVATE)
                .edit()
                .putStringSet(CapturaNotificacoesService.CHAVE_PACKAGES, packages.toSet())
                .apply()
            Unit
        }

        /** Lê e limpa a fila persistente; cada item é uma string JSON. */
        Function("drenarFila") {
            val context = appContext.reactContext ?: return@Function emptyList<String>()
            FilaNotificacoes.drenar(context)
        }
    }

    init {
        instancia = this
    }

    companion object {
        private var instancia: CapturaNotificacoesModule? = null
        private val pronto = AtomicBoolean(false)
        private val mainHandler = Handler(Looper.getMainLooper())

        fun avisarFilaAtualizada() {
            mainHandler.post {
                try {
                    if (pronto.get()) instancia?.sendEvent("onFilaAtualizada", emptyMap<String, Any>())
                } catch (e: Exception) {
                    e.printStackTrace()
                }
            }
        }
    }
}
