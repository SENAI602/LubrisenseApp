#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include <ArduinoJson.h>

#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_UUID "1c95d5e3-d8f7-413a-bf3d-7a2e5d7be87e"

BLEServer* pServer = NULL;
BLECharacteristic* pCharacteristic = NULL;
bool deviceConnected = false;
bool oldDeviceConnected = false;

// Callback para saber quando conectou/desconectou
class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
      deviceConnected = true;
      Serial.println("[BLE] App Conectado!");
    };

    void onDisconnect(BLEServer* pServer) {
      deviceConnected = false;
      Serial.println("[BLE] App Desconectado.");
    }
};

// Callback para receber dados (Quando você clica em "Salvar" no App)
class MyCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      String rxValue = pCharacteristic->getValue().c_str(); // Converte para String do Arduino

      if (rxValue.length() > 0) {
        Serial.println("\n========= [DADOS RECEBIDOS] =========");
        Serial.println("JSON Bruto recebido:");
        Serial.println(rxValue);
        Serial.println("-------------------------------------");

        // Tenta interpretar o JSON para provar que a lógica do App está certa
        JsonDocument doc;
        DeserializationError error = deserializeJson(doc, rxValue);

        if (error) {
          Serial.print("[ERRO] JSON Inválido: ");
          Serial.println(error.c_str());
          return;
        }

        // 1. Verifica o Envelope (Comando)
        const char* comando = doc["comando"];
        Serial.print("Comando identificado: ");
        Serial.println(comando);

        if (strcmp(comando, "set_config") == 0) {
           Serial.println(">> Ação: Configurar Dispositivo...");
           
           // 2. Acessa o Payload (Onde estão os dados reais)
           JsonObject payload = doc["payload"];
           
           if (!payload.isNull()) {
              // Extrai os dados para validar se a tela do App funcionou
              int volume = payload["Volume"];
              int duracao = payload["Intervalo"];
              int tipoDuracao = payload["TipoIntervalo"];
              int frequencia = payload["Frequencia"];       // <--- O CAMPO NOVO
              int tipoFrequencia = payload["TipoFrequencia"]; // <--- O CAMPO NOVO
              int tipoConfig = payload["TipoConfig"];

              Serial.println("--- DADOS EXTRAÍDOS ---");
              Serial.print("Modo (1=Basico, 2=Avancado): "); Serial.println(tipoConfig);
              Serial.print("Volume: "); Serial.println(volume);
              Serial.print("Duração Total: "); Serial.print(duracao); Serial.print(" (Tipo: "); Serial.print(tipoDuracao); Serial.println(")");
              Serial.print("Frequência (Gatilho): "); Serial.print(frequencia); Serial.print(" (Tipo: "); Serial.print(tipoFrequencia); Serial.println(")");
              
              if (frequencia > 0) {
                 Serial.println("✅ SUCESSO: O campo 'Frequencia' chegou corretamente!");
              } else {
                 Serial.println("❌ ALERTA: O campo 'Frequencia' veio vazio ou zero!");
              }

           } else {
             Serial.println("❌ ERRO: Payload vazio!");
           }
        } else {
          Serial.println("Comando desconhecido.");
        }
        Serial.println("=====================================\n");
      }
    }
};

void setup() {
  Serial.begin(115200);
  Serial.println("Iniciando ESP32 Simulador de Teste...");

  // 1. Cria o dispositivo BLE
  BLEDevice::init("LubriSense-Teste"); // Nome que vai aparecer no App

  // 2. Cria o Servidor
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new MyServerCallbacks());

  // 3. Cria o Serviço
  BLEService *pService = pServer->createService(SERVICE_UUID);

  // 4. Cria a Característica
  pCharacteristic = pService->createCharacteristic(
                      CHARACTERISTIC_UUID,
                      BLECharacteristic::PROPERTY_READ   |
                      BLECharacteristic::PROPERTY_WRITE  |
                      BLECharacteristic::PROPERTY_NOTIFY
                    );

  pCharacteristic->setCallbacks(new MyCallbacks());

  // Adiciona um descritor (necessário para notificações em alguns celulares)
  pCharacteristic->addDescriptor(new BLE2902());

  // 5. Inicia o serviço
  pService->start();

  // 6. Começa a anunciar (aparecer no radar)
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
  pAdvertising->setMinPreferred(0x0);  // set value to 0x00 to not advertise this parameter
  BLEDevice::startAdvertising();
  Serial.println("Aguardando conexão do App MAUI...");
}

void loop() {
  // Lógica simples para reiniciar o anúncio se desconectar
  if (!deviceConnected && oldDeviceConnected) {
      delay(500); // dá um tempo para a pilha bluetooth se organizar
      pServer->startAdvertising(); // Reinicia o anúncio
      Serial.println("Reiniciando anúncio (Advertising)...");
      oldDeviceConnected = deviceConnected;
  }
  // Lógica para atualizar status de conexão
  if (deviceConnected && !oldDeviceConnected) {
      oldDeviceConnected = deviceConnected;
  }
}