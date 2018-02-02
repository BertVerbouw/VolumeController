package com.verbouw.bert.volumecontroller;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.net.wifi.ScanResult;
import android.net.wifi.WifiManager;
import android.os.AsyncTask;
import android.os.Handler;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.view.inputmethod.InputMethodManager;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;

import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;

public class MainActivity extends AppCompatActivity {
    private LinearLayout volumeControllerContainer;
    private ScrollView scrollView;
    private LinearLayout inputLayout;
    private LinearLayout splashLayout;
    private EditText ipTextBox;
    private SharedPreferences preferences;
    TcpClient mTcpClient;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        volumeControllerContainer = this.findViewById(R.id.vcContainer);
        scrollView = this.findViewById(R.id.scrollViewer);
        ipTextBox = this.findViewById(R.id.ipaddress);
        inputLayout = this.findViewById(R.id.inputLayout);
        splashLayout = this.findViewById(R.id.LoadingScreen);

        preferences = getSharedPreferences("label", 0);
        String ip = preferences.getString("ipAddress", "");
        Variables.ipAddress = ip;
        ipTextBox.setText(ip);
        new ConnectTask().execute("");
    }

    private void loadVolumeControllers(JSONArray data) {
        RemoveVolumeControllers(data);
        AddVolumeControllers(data);
    }

    private void AddVolumeControllers(JSONArray data) {
        for (int i = 0; i < data.length(); i++) {
            try {
                JSONObject object = data.getJSONObject(i);
                if(!viewContainsPid(object.getInt("Pid"))){
                    volumeControllerContainer.addView(new VolumeController(this, object.getBoolean("IsMuted"), object.getInt("Volume"), object.getString("Name"), object.getInt("Pid")));
                }else{
                    EditVolumeController(object);
                }
            }
            catch (JSONException e) {
            }
        }
    }

    private void EditVolumeController(JSONObject object) {
        for(int i=0; i<volumeControllerContainer.getChildCount();i++){
            try {
                if(((VolumeController)volumeControllerContainer.getChildAt(i)).getPid() == object.getInt("Pid")){
                    VolumeController controller = (VolumeController)volumeControllerContainer.getChildAt(i);
                    controller.setVolume(object.getInt("Volume"));
                    controller.setIsMuted(object.getBoolean("IsMuted"));
                    controller.setName(object.getString("Name"));
                    controller.initializeValues();
                }
            } catch (JSONException e) {
            }
        }
    }

    private void RemoveVolumeControllers(JSONArray data) {
        for(int i=0; i<volumeControllerContainer.getChildCount();i++){
            if(!dataContainsChildPid((VolumeController)volumeControllerContainer.getChildAt(i), data)){
                volumeControllerContainer.removeViewAt(i);
            }
        }
    }

    private boolean dataContainsChildPid(VolumeController child, JSONArray data) {
        for (int i = 0; i < data.length(); i++) {
            try {
                JSONObject object = data.getJSONObject(i);
                if(object.getInt("Pid") == child.getPid()){
                    return true;
                }
            }
            catch (JSONException e) {
            }
        }
        return false;
    }

    private boolean viewContainsPid(int pid) {
        for(int i=0; i<volumeControllerContainer.getChildCount();i++){
            if(((VolumeController)volumeControllerContainer.getChildAt(i)).getPid() == pid){
                return true;
            }
        }
        return false;
    }

    public void retryButtonClick(View view) {
        splashLayout.setVisibility(View.VISIBLE);
        hideKeyboard(this);
        Variables.ipAddress = ipTextBox.getText().toString();
        preferences.edit().putString("ipAddress", Variables.ipAddress).commit();
        //sends the message to the server
        new ConnectTask().execute("");
    }

    public static void hideKeyboard(Activity activity) {
        InputMethodManager imm = (InputMethodManager) activity.getSystemService(Activity.INPUT_METHOD_SERVICE);
        //Find the currently focused view, so we can grab the correct window token from it.
        View view = activity.getCurrentFocus();
        //If no view currently has focus, create a new one, just so we can grab a window token from it
        if (view == null) {
            view = new View(activity);
        }
        imm.hideSoftInputFromWindow(view.getWindowToken(), 0);
    }

    public void setMute(int pid, boolean isMuted){
        new SendOperation().executeOnExecutor(AsyncTask.THREAD_POOL_EXECUTOR,"mute*"+pid+"*"+isMuted);
    }


    public void setVolume(int pid, int volume){
        new SendOperation().executeOnExecutor(AsyncTask.THREAD_POOL_EXECUTOR,"vol*"+pid+"*"+volume);
    }

    private class SendOperation extends AsyncTask<String, Void, String> {
        @Override
        protected String doInBackground(String... params) {
            if(mTcpClient != null) {
                mTcpClient.sendMessage(params[0]);
            }
            return null;
        }

        @Override
        protected void onPostExecute(String result) {
        }

        @Override
        protected void onPreExecute() {
        }

        @Override
        protected void onProgressUpdate(Void... values) {
        }
    }

    private class ConnectTask extends AsyncTask<String, String, TcpClient> {

        @Override
        protected TcpClient doInBackground(String... message) {

            //we create a TCPClient object
            mTcpClient = new TcpClient(new TcpClient.OnMessageReceived() {
                @Override
                //here the messageReceived method is implemented
                public void messageReceived(String message) {
                    //this method calls the onProgressUpdate
                    publishProgress(message);
                }
            });
            mTcpClient.run();
            scrollView.setVisibility(View.INVISIBLE);
            inputLayout.setVisibility(View.VISIBLE);
            return null;
        }

        @Override
        protected void onProgressUpdate(String... values) {
            super.onProgressUpdate(values);
            //response received from server
            try {
                loadVolumeControllers(new JSONArray(values[0]));
                scrollView.setVisibility(View.VISIBLE);
                inputLayout.setVisibility(View.INVISIBLE);
                splashLayout.setVisibility(View.INVISIBLE);
            } catch (JSONException e) {
                e.printStackTrace();
            }
            //process server response here....

        }
    }
}


