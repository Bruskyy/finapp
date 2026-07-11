package expo.modules.capturanotificacoes

import android.content.Context
import java.io.File

/**
 * Fila persistente de notificações capturadas (fase 2 do
 * ITEM-CAPTURA-NOTICACOES.md): uma linha JSON por notificação, num arquivo
 * no armazenamento interno do app. O NotificationListenerService escreve
 * aqui mesmo quando o runtime JS está morto; o app drena o arquivo ao
 * abrir. Sincronizado num lock único porque escrita (serviço) e drenagem
 * (módulo) rodam em threads diferentes.
 */
object FilaNotificacoes {
    private const val NOME_ARQUIVO = "fila-notificacoes-capturadas.jsonl"
    private const val MAX_LINHAS = 200
    private val lock = Any()

    private fun arquivo(context: Context) = File(context.filesDir, NOME_ARQUIVO)

    fun adicionar(context: Context, linhaJson: String) {
        synchronized(lock) {
            val arquivo = arquivo(context)
            arquivo.appendText(linhaJson + "\n")
            // Teto pra fila não crescer sem fim se o usuário ficar semanas sem
            // abrir o app: mantém só as MAX_LINHAS mais recentes.
            val linhas = arquivo.readLines()
            if (linhas.size > MAX_LINHAS) {
                arquivo.writeText(linhas.takeLast(MAX_LINHAS).joinToString("\n") + "\n")
            }
        }
    }

    /** Lê todas as linhas e limpa o arquivo (drenagem atômica sob o lock). */
    fun drenar(context: Context): List<String> {
        synchronized(lock) {
            val arquivo = arquivo(context)
            if (!arquivo.exists()) return emptyList()
            val linhas = arquivo.readLines().filter { it.isNotBlank() }
            arquivo.delete()
            return linhas
        }
    }
}
